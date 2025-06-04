using FluentResults;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Restorator.DataAccess.Data;
using Restorator.DataAccess.Data.Entities;
using Restorator.Domain.Models.Enums;
using Restorator.Domain.Models.Reservations;
using Restorator.Domain.Models.Restaurant;
using Restorator.Domain.Services;
using Restorator.Mail.Models.Templates;
using Restorator.Mail.Services;
using System.Collections.Immutable;
using Roles = Restorator.DataAccess.Data.Entities.Enums.Roles;

namespace Restorator.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly RestoratorDbContext _context;
        private readonly IUserManager _userManager;
        private readonly IMailService _mailService;
        public ReservationService(RestoratorDbContext context,
                                  IUserManager userManager,
                                  IMailService mailService)
        {
            _context = context;
            _userManager = userManager;
            _mailService = mailService;
        }

        public async Task<Result> CancelReservation(int reservationId)
        {
            if (!_userManager.TryGetUserId(out var userId))
                return Result.Fail("Не удалось получить id пользователя");

            var reseravation = await _context.Reservations.Include(r => r.User)
                                                          .SingleOrDefaultAsync(r => r.Id == reservationId);

            if (reseravation is null)
                return Result.Fail("Бронирование не найдено");

            var user = await _context.Users.AsNoTracking()
                                           .Include(u => u.Role)
                                           .SingleOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return Result.Fail("Пользователь не найден");

            if (user.Role == Roles.Manager)
            {
                var template = await _context.Reservations.AsNoTracking()
                                                          .Where(r => r.Id == reservationId)
                                                          .Select(r => new ReservationCanceledTemplate(r.User.Email,
                                                                                                           r.User.Username,
                                                                                                           r.ReservationStart,
                                                                                                           r.Restaurant.Name)).SingleAsync();

                await _mailService.SendMailAsync(template);
            }
            else if (reseravation.User.Id != user.Id)
                return Result.Fail("Недостаточно прав для отмены бронирования");

            reseravation.Canceled = true;

            _context.Reservations.Update(reseravation);

            await _context.SaveChangesAsync();

            return Result.Ok();
        }
        public async Task<Result<ReservationInfoDTO>> GetReservationInfo(int reservationId)
        {
            var reservation = await _context.Reservations.AsNoTracking()
                                                         .Select(r => new ReservationInfoDTO
                                                         {
                                                             Id = r.Id,
                                                             UserId = r.User.Id,
                                                             Username = r.User.Username,
                                                             RestaurantId = r.Restaurant.Id,
                                                             RestaurantName = r.Restaurant.Name,
                                                             ReservationStart = r.ReservationStart,
                                                             ReservationEnd = r.ReservationEnd
                                                         }).SingleOrDefaultAsync(r => r.Id == reservationId);

            if (reservation is null)
                return Result.Fail("Бронирование не найдено");

            return Result.Ok(reservation);
        }
        public async Task<Result<IReadOnlyCollection<ReservationInfoDTO>>> GetReservations(GetReservationsDTO model)
        {
            var predicate = PredicateBuilder.New<Reservation>(r => r.ReservationEnd.Date == model.SelectedDate.ToDateTime(TimeOnly.MinValue)
                                                                   || r.ReservationStart.Date == model.SelectedDate.ToDateTime(TimeOnly.MinValue));

            if (model.UserId.HasValue)
                predicate = predicate.And(r => r.User.Id == model.UserId.Value);

            if (model.RestaurantId.HasValue)
                predicate = predicate.And(r => r.Restaurant.Id == model.RestaurantId);

            if (model.SkipCanceled.HasValue)
                predicate = predicate.And(r => r.Canceled != model.SkipCanceled.Value);

            return await _context.Reservations.AsNoTracking()
                                              .Where(predicate)
                                              .Select(r => new ReservationInfoDTO
                                              {
                                                  Id = r.Id,
                                                  UserId = r.User.Id,
                                                  Username = r.User.Username,
                                                  RestaurantId = r.Restaurant.Id,
                                                  RestaurantName = r.Restaurant.Name,
                                                  ReservationStart = r.ReservationStart,
                                                  ReservationEnd = r.ReservationEnd,
                                                  Canceled = r.Canceled
                                              }).ToListAsync();
        }
        public async Task<Result<RestaurantPlanDTO>> GetRestaurantReservationPlan(GetRestaurantPlanDTO model)
        {
            if (!_userManager.TryGetUserId(out var userId))
                return Result.Fail("Не удалось получить id пользователя");

            var restaurant = await _context.Restaurants.AsNoTracking()
                                                       .Include(x => x.Template)
                                                       .SingleOrDefaultAsync(x => x.Id == model.RestaurantId);

            if (restaurant is null)
                return Result.Fail("Ресторан не найден");

            var reservations = await _context.Entry(restaurant)
                 .Collection(x => x.Reservations)
                 .Query()
                 .Where(r => !r.Canceled
                             && r.ReservationStart < model.ReservationEndDate
                             && model.ReservationStartDate < r.ReservationEnd)
                 .Select(r => new ReservationDTO
                 {
                     Id = r.Id,
                     UserId = r.User.Id,
                     TableId = r.Table.Id,
                 }).ToListAsync();

            var tables = await _context.Entry(restaurant.Template)
                .Collection(x => x.Tables)
                .Query()
                .Select(t => new ReservationTableDTO
                {
                    Id = t.Id,
                    Height = t.Template.Height,
                    Width = t.Template.Width,
                    Rotation = t.Template.Rotation,
                    X = t.X,
                    Y = t.Y,
                }).ToListAsync();

            foreach (var table in tables)
            {
                var reservation = reservations.FirstOrDefault(x => x.TableId == table.Id);

                if (reservation is null)
                {
                    table.State = TableStates.Avaible;

                    continue;
                }

                table.State = reservation.UserId == userId ? TableStates.OccupiedByUser : TableStates.OccupiedByOther;
                table.ReservationId = reservation.Id;
            }

            var plan = new RestaurantPlanDTO
            {
                Id = restaurant.Id,
                Scheme = restaurant.Template.Image,
                BeginWorkTime = restaurant.BeginWorkTime,
                EndWorkTime = restaurant.EndWorkTime,
                Tables = tables,
            };

            return Result.Ok(plan);
        }
        public async Task<Result<bool>> IsReservationOwner(int reservationId)
        {
            if (!_userManager.TryGetUserId(out var userId))
                return Result.Fail("Не удалось получить id пользователя");

            if (!await _context.Users.AnyAsync(u => u.Id == userId))
                return Result.Fail("Пользователя не существует");

            if (!await _context.Reservations.AnyAsync(r => r.Id == reservationId))
                return Result.Fail("Бронирование не существует");

            return Result.Ok(await _context.Reservations.AnyAsync(r => r.User.Id == userId && r.Id == reservationId));
        }
        public async Task<Result<int>> CreateReservation(CreateRestaurantReservationDTO model)
        {
            if (!_userManager.TryGetUserId(out var userId))
                return Result.Fail("Не удалось получить id пользователя");

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return Result.Fail("Пользователя не существует");

            var tables = await _context.Tables.Where(t => model.ReservedTables.Contains(t.Id))
                                              .ToListAsync();

            if (tables is null || tables.Count != model.ReservedTables.Count)
                return Result.Fail("Стол не найден");

            var restaurant = await _context.Restaurants.SingleOrDefaultAsync(r => r.Id == model.RestaurantId);

            //задачка Aka "Этот стол принадлежит этому ресторану?"
            //&& r.Template.Tables.All(t => tables.Contains(t)));

            if (restaurant is null)
                return Result.Fail("Ресторан не найден");

            foreach (var table in tables)
            {
                var reservation = new Reservation()
                {
                    User = user,
                    Restaurant = restaurant,
                    Table = table,
                    ReservationStart = model.ReservationStartDate,
                    ReservationEnd = model.ReservationEndDate,
                };

                _context.Reservations.Add(reservation);
            }

            await _context.SaveChangesAsync();

            return Result.Ok(restaurant.Id);
        }

        public async Task<Result<IReadOnlyCollection<ReservationInfoDTO>>> GetOwnedReservations(GetOwnedReservationsDTO model)
        {
            if (!_userManager.TryGetUserId(out var userId))
                return Result.Fail("Не удалось получить id пользователя");

            var predicate = PredicateBuilder.New<Reservation>(r => r.User.Id == userId && r.ReservationEnd.Date == model.SelectedDate.ToDateTime(TimeOnly.MinValue)
                                                                  || r.ReservationStart.Date == model.SelectedDate.ToDateTime(TimeOnly.MinValue));

            if (model.RestaurantId.HasValue)
                predicate = predicate.And(r => r.Restaurant.Id == model.RestaurantId);

            if (model.SkipCanceled.HasValue)
                predicate = predicate.And(r => r.Canceled != model.SkipCanceled.Value);

            return await _context.Reservations.AsNoTracking()
                                              .Where(predicate)
                                              .Select(r => new ReservationInfoDTO
                                              {
                                                  Id = r.Id,
                                                  UserId = r.User.Id,
                                                  Username = r.User.Username,
                                                  RestaurantId = r.Restaurant.Id,
                                                  RestaurantName = r.Restaurant.Name,
                                                  ReservationStart = r.ReservationStart,
                                                  ReservationEnd = r.ReservationEnd,
                                                  Canceled = r.Canceled
                                              }).ToListAsync();
        }

        public class ReservationDTO
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int TableId { get; set; }
        }
    }
}
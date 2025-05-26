﻿using Restorator.Application.Models;

namespace Restorator.Application.Services.Abstract
{
    public interface IRestaurantFilesManager
    {
        Task<RestaurantFilesInfoDTO> CreateRestaurantFolder(string restaurantName, IEnumerable<byte[]> images, byte[] menu);
        Task<RestaurantFilesInfoDTO> UpdateRestaurantFolder(string restaurantName, IEnumerable<byte[]> images, byte[] menu);
        Task DeleteRestaurantFolder(string restaurantName);
    }
}
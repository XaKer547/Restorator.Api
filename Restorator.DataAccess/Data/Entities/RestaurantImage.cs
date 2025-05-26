using System.ComponentModel.DataAnnotations;

namespace Restorator.DataAccess.Data.Entities
{
    public class RestaurantImage
    {
        [Key] //wtf bro
        public int Id { get; set; }
        public string Image { get; set; }
    }
}

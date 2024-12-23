using System;

namespace ProductManagement.Application.DTOs
{
    public class ProductReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Availability { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

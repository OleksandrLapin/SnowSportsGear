using API.DTOs;
using Core.Entities;

namespace API.Extensions;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            PictureUrl = product.PictureData != null && product.PictureContentType != null
                ? $"data:{product.PictureContentType};base64,{Convert.ToBase64String(product.PictureData)}"
                : product.PictureUrl ?? string.Empty,
            Type = product.Type,
            Brand = product.Brand,
            QuantityInStock = product.QuantityInStock
        };
    }
}

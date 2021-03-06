﻿using Database;
using Database.Model;
using Fetcher.Model;
using Fetcher.Model.Thief;

namespace Core.Mapper
{
    public static class ProductMapper
    {
        public static Database.Model.Product ConvertToDatabaseProduct(this Fetcher.Model.Thief.Product product, int prodCode)
        {
            var prod = new Database.Model.Product()
            {
                Name = product.Name,
                ExternalProductId = prodCode,
                Saleable = product.Saleable == 1,
                Code = product.Code,
                BrandId = product.BrandId,
                CategoryId = product.SubCategoryId,
                SpecialPrice = (product.SpecialPrice ?? 0) / 100,
                PreviousSpecialPrice = (product.PreviousSpecialPrice ?? 0) / 100,
                ListPrice = (product.ListPrice ?? 0) / 100,
                PreviousListPrice = (product.PreviousListPrice ?? 0) / 100
            };
            return prod;
        }
    }
}

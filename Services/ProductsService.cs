using Database;
using Database.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class ProductService
    {
        private readonly StackGameContext stackGameContext;
        public ProductService(StackGameContext stackGameContext)
        {
            this.stackGameContext = stackGameContext;
        }

        public void InsertUpdateProduct(Product product)
        {
            if (stackGameContext.Products.FirstOrDefault(p => p.ExternalProductId == product.ExternalProductId) == null)
            {
                stackGameContext.Products.Add(product);
            }
            else
            {
                stackGameContext.Products.Update(product);
            }
            stackGameContext.SaveChanges();
        }       
    }
}

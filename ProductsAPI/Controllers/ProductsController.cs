using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WebShopLibrary;
using WebShopLibrary.Database;

namespace ProductsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductRepositoryDb _productRepository;

        public ProductsController(ProductRepositoryDb productRepository)
        {
            _productRepository = productRepository;
        }

        // GET: api/<ProductsController>
        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAll()
        {
            var products = _productRepository.GetAll();
            if (!products.Any()) return NoContent();
            return Ok(products);
        }

        // GET api/<ProductsController>/5
        [HttpGet("{id}")]
        public ActionResult<Product> GetById(int id)
        {
            var product = _productRepository.Get(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // POST api/<ProductsController>
        //[HttpPost]
        //public async Task<ActionResult<Product>> Post([FromForm] string name, [FromForm] string model, [FromForm] double price, [FromForm] IFormFile? file)
        //{
        //    try
        //    {
        //        if (file == null || file.Length == 0)
        //            return BadRequest("No file uploaded.");

        //        // Read the file as binary data
        //        byte[] fileData;
        //        using (var memoryStream = new MemoryStream())
        //        {
        //            await file.CopyToAsync(memoryStream);
        //            fileData = memoryStream.ToArray();
        //        }

        //        var newProduct = new Product
        //        {
        //            Name = name,
        //            Model = model, // Replace with actual model value
        //            Price = price, // Replace with actual price value
        //            ImageData = fileData
        //        };

        //        var createdProduct = _productRepository.Add(newProduct);
        //        return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}


        // DELETE api/<ProductsController>/5
        [HttpDelete("{id}")]
        public ActionResult<Product> Delete(int id)
        {
            var product = _productRepository.Get(id);
            if (product == null) return NotFound();
            _productRepository.Remove(id);
            return Ok(product);
        }

        private async Task<byte[]> ConvertToByteArray(IFormFile file)
        {
            if (file == null) return null!; // Use null-forgiving operator to suppress warning

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
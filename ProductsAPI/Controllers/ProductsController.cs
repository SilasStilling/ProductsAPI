using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<Product>> Post([FromForm] string name, [FromForm] string model, [FromForm] double price, [FromForm] IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");

                // Read the file as binary data
                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                var newProduct = new Product
                {
                    Name = name,
                    Model = model,
                    Price = price,
                    ImageData = fileData
                };

                var createdProduct = _productRepository.Add(newProduct);
                return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<ProductsController>/5
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public ActionResult<Product> Put(int id, [FromForm] string name, [FromForm] string model, [FromForm] double price, [FromForm] IFormFile? file)
        {
            var product = _productRepository.Get(id);
            if (product == null) return NotFound();

            product.Name = name;
            product.Model = model;
            product.Price = price;

            if (file != null)
            {
                product.ImageData = ConvertToByteArray(file).Result;
            }

            _productRepository.Update(product);
            return Ok(product);
        }


        // DELETE api/<ProductsController>/5
        [Authorize(Roles = "admin")]
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
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
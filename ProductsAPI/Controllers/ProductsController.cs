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
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(model) || price <= 0)
                return BadRequest("Invalid product data.");

            try
            {
                byte[] fileData = file != null ? await ConvertToByteArray(file) : new byte[0];

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
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        // PUT api/<ProductsController>/5
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> Put(int id, [FromForm] string name, [FromForm] string model, [FromForm] double price, [FromForm] IFormFile? file)
        {
            var product = _productRepository.Get(id);
            if (product == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(model) || price <= 0)
                return BadRequest("Invalid product data.");

            product.Name = name;
            product.Model = model;
            product.Price = price;

            if (file != null)
            {
                product.ImageData = await ConvertToByteArray(file);
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
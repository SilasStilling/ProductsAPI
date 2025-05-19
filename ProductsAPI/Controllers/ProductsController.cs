using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebShopLibrary;
using WebShopLibrary.Database;

namespace ProductsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductRepositoryDb _productRepository;
        private readonly LogService _logService;

        public ProductsController(ProductRepositoryDb productRepository, LogService logService)
        {
            _productRepository = productRepository;
            _logService = logService;
        }

        private string GetCurrentUsername() => User.Identity?.Name ?? "Anonymous";

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
            if (product == null)
            {
                _logService.LogAsync("GetProductById", GetCurrentUsername(), "Failed", "NotFound").Wait();
                return NotFound();
            }
            _logService.LogAsync("GetProductById", GetCurrentUsername(), "Success").Wait();
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
                await _logService.LogAsync("CreateProduct", GetCurrentUsername(), "Success");
                return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
            }
            catch (ArgumentException ex)
            {
                await _logService.LogAsync("CreateProduct", GetCurrentUsername(), "Failed", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<ProductsController>/5
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> Put(int id, [FromForm] string name, [FromForm] string model, [FromForm] double price, [FromForm] IFormFile? file)
        {
            var product = _productRepository.Get(id);
            if (product == null)
            {
                await _logService.LogAsync("UpdateProduct", GetCurrentUsername(), "Failed", "NotFound");
                return NotFound();
            }

            product.Name = name;
            product.Model = model;
            product.Price = price;

            if (file != null)
            {
                product.ImageData = await ConvertToByteArray(file);
            }

            _productRepository.Update(product);
            await _logService.LogAsync("UpdateProduct", GetCurrentUsername(), "Success");
            return Ok(product);
        }

        // DELETE api/<ProductsController>/5
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<Product>> Delete(int id)
        {
            var product = _productRepository.Get(id);
            if (product == null)
            {
                await _logService.LogAsync("DeleteProduct", GetCurrentUsername(), "Failed", "NotFound");
                return NotFound();
            }
            _productRepository.Remove(id);
            await _logService.LogAsync("DeleteProduct", GetCurrentUsername(), "Success");
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

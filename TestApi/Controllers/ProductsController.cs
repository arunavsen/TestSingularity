using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestApi.Models;

namespace TestApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.Where(p=>p.DeleteStatus == false).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null || product.DeleteStatus == true)
            {
                return NotFound();
            }

            return product;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            // If the product's Deleted or Locked, we can not edit that.
            var p = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (p.DeleteStatus == true || p.LockStatus==true)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        // DeleteStatus action
        [HttpDelete("{id}")]
        public async Task<ActionResult<Product>> DeleteProduct(int id)
        {
            // This will return only that product which has the false DeleteStatus. It is OK product.
            var product = await _context.Products.FirstOrDefaultAsync(x=>x.DeleteStatus != true && x.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // As we can not delete any product, we have to lock the product. If LockStatus is true, that means you can not delete that product.
            if (product.LockStatus == true)
            {
                return BadRequest();
            }

            product.DeleteStatus = true;
            await _context.SaveChangesAsync();

            return product;
        }

        [HttpPost("LockProduct/{id}")]
        public async Task<ActionResult<Product>> LockProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || product.DeleteStatus == true)
            {
                return NotFound();
            }

            product.LockStatus = product.LockStatus != true;
            await _context.SaveChangesAsync();

            return product;
        }

        [HttpPost("RecoverProduct/{id}")]
        public async Task<ActionResult<Product>> RecoverProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.DeleteStatus = false;
            product.LockStatus = false;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("GetTrashProducts")]
        public async Task<ActionResult<IEnumerable<Product>>> GetTrashProducts()
        {
            return await _context.Products.Where(m => m.DeleteStatus == true).ToListAsync();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}

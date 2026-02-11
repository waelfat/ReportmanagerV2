using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.ViewModels;

namespace reportmangerv2.Controllers;

public class SchemaController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<SchemaController> _logger;
    public SchemaController(AppDbContext context, ILogger<SchemaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Schema
    public async Task<IActionResult> Index()
    {
        var schemas = await _context.Schemas.ToListAsync();
        var viewModels = schemas.Select(s => new SchemaViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatedAt = s.CreatedAt,
            Host = s.Host,
            Port = s.Port,
            ServiceName = s.ServiceName,
            UserId = s.UserId,
            Password = s.Password
        }).ToList();
        return View(viewModels);
    }

    // GET: Schema/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (id == null) return NotFound();
        var schema = await _context.Schemas.FirstOrDefaultAsync(m => m.Id == id);
        if (schema == null) return NotFound();
        var viewModel = new SchemaViewModel
        {
            Id = schema.Id,
            Name = schema.Name,
            Description = schema.Description,
            CreatedAt = schema.CreatedAt,
            Host = schema.Host,
            Port = schema.Port,
            ServiceName = schema.ServiceName,
            UserId = schema.UserId,
            Password = schema.Password
        };
        return View(viewModel);
    }

    // GET: Schema/Create
    public IActionResult Create()
    {
        return View(new CreateSchemaViewModel());
    }

    // POST: Schema/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create( CreateSchemaViewModel model)
    {
        if (ModelState.IsValid)
        {
            var schema = new Schema
            {
                Name = model.Name,
                Description = model.Description,
                Host = model.Host,
                Port = model.Port,
                ServiceName = model.ServiceName,
                UserId = model.UserId,
                Password = model.Password
            };
          try
            {
                _=await schema.ValidateConnectionAsync();
                    
                  
              
            }
            catch(Exception e)
            {
                ModelState.AddModelError(string.Empty, $"Connection test failed: {e.Message}");
                ViewBag.ErrorMessage = $"Connection test failed: {e.Message}";
                return View(model);
            }
            _context.Add(schema);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Schema/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();
        var schema = await _context.Schemas.FindAsync(id);
        if (schema == null) return NotFound();
        var viewModel = new EditSchemaViewModel
        {
            Id = schema.Id,
            Name = schema.Name,
            Description = schema.Description,
            Host = schema.Host,
            Port = schema.Port,
            ServiceName = schema.ServiceName,
            UserId = schema.UserId,
            Password = schema.Password
        };
        return View(viewModel);
    }

    // POST: Schema/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditSchemaViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var schema = await _context.Schemas.FindAsync(id);
            if (schema == null) return NotFound();
            schema.Name = model.Name;
            schema.Description = model.Description;
            schema.Host = model.Host;
            schema.Port = model.Port;
            schema.ServiceName = model.ServiceName;
            schema.UserId = model.UserId;
            schema.Password = model.Password;
           if(!await schema.ValidateConnectionAsync())
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the database with the provided details.");
                ViewBag.ErrorMessage = "Unable to connect to the database with the provided details.";
                return View(model);
            }
            try
            {
                _context.Update(schema);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SchemaExists(schema.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Schema/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();
        var schema = await _context.Schemas.FirstOrDefaultAsync(m => m.Id == id);
        if (schema == null) return NotFound();
        var viewModel = new SchemaViewModel
        {
            Id = schema.Id,
            Name = schema.Name,
            Description = schema.Description,
            CreatedAt = schema.CreatedAt,
            Host = schema.Host,
            Port = schema.Port,
            ServiceName = schema.ServiceName,
            UserId = schema.UserId,
            Password = schema.Password
        };
        return View(viewModel);
    }

    // POST: Schema/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var schema = await _context.Schemas.FindAsync(id);
        if (schema != null)
        {
            _context.Schemas.Remove(schema);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool SchemaExists(string id)
    {
        return _context.Schemas.Any(e => e.Id == id);
    }
    // GET: test connection
    private async Task <bool> TestConnection(Schema schema)
    {
        try
        {
            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(
                $"User Id={schema.UserId};Password={schema.Password};Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={schema.Host})(PORT={schema.Port}))(CONNECT_DATA=(SERVICE_NAME={schema.ServiceName})))");
            await connection.OpenAsync();
            return true;
        }
        catch(Exception e)
        {
            _logger.LogError(e, "Connection test failed for schema {SchemaName}", schema.Name);
            return false;
        }
    }
}

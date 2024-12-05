using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using SocialMediaApi.Models;

namespace SocialMediaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public PostsController(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;

        // Accéder au conteneur Cosmos DB
        var database = _cosmosClient.GetDatabase("db_socialapp");
        _container = database.GetContainer("Publication");
    }

    // GET: api/posts
    [HttpGet]
    public async Task<IActionResult> GetAllPosts()
    {
        var query = _container.GetItemQueryIterator<Publication>(new QueryDefinition("SELECT * FROM c"));
        var results = new List<Publication>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return Ok(results);
    }

    // GET: api/posts/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPostById(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Publication>(id, new PartitionKey(id));
            return Ok(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Post introuvable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur interne : {ex.Message}");
            return StatusCode(500, "Erreur interne du serveur.");
        }
    }

    // POST: api/posts
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] Publication post)
    {
        try
        {
            if (post == null)
            {
                return BadRequest("Le corps de la requête est vide.");
            }

            // Validation des données
            if (string.IsNullOrWhiteSpace(post.Titre) || string.IsNullOrWhiteSpace(post.Contenu))
            {
                return BadRequest("Les champs 'Titre' et 'Contenu' sont obligatoires.");
            }

            // Mapper IdPublication vers id pour Cosmos DB
            Publication document = new Publication()
            {
                id = Guid.NewGuid().ToString(), // Génération d'un ID unique
                Titre = post.Titre,
                Contenu = post.Contenu,
                Media = post.Media,
                DateCreation = DateTime.UtcNow
            };

            // Ajouter dans Cosmos DB
            await _container.CreateItemAsync(document, new PartitionKey(document.id));

            return CreatedAtAction(nameof(GetPostById), new { id = document.id }, document);
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"Erreur Cosmos DB : {ex.Message}");
            return StatusCode(500, "Erreur lors de la communication avec Cosmos DB.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur interne : {ex.Message}");
            return StatusCode(500, "Erreur interne du serveur.");
        }
    }

    // PUT: api/posts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(string id, [FromBody] Publication post)
    {
        try
        {
            if (post == null)
            {
                return BadRequest("Le corps de la requête est vide.");
            }

            // Vérifier si la publication existe
            var existingItem = await _container.ReadItemAsync<Publication>(id, new PartitionKey(id));
            if (existingItem == null)
            {
                return NotFound("Post introuvable.");
            }

            // Mettre à jour les champs de la publication
            post.id = id; // Conserver l'ID
            post.DateCreation = existingItem.Resource.DateCreation; // Conserver la date de création d'origine

            // Mettre à jour l'élément dans Cosmos DB
            await _container.ReplaceItemAsync(post, id, new PartitionKey(id));

            return Ok(post);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Post introuvable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur interne : {ex.Message}");
            return StatusCode(500, "Erreur interne du serveur.");
        }
    }

    // DELETE: api/posts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(string id)
    {
        try
        {
            // Supprimer l'élément dans Cosmos DB
            await _container.DeleteItemAsync<Publication>(id, new PartitionKey(id));
            return NoContent(); // Retourne un statut 204 No Content
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Post introuvable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur interne : {ex.Message}");
            return StatusCode(500, "Erreur interne du serveur.");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using SocialMediaApi.Models;

namespace SocialMediaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentairesController : ControllerBase
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public CommentairesController(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;

        // Acc�der au conteneur Cosmos DB
        var database = _cosmosClient.GetDatabase("db_socialapp");
        _container = database.GetContainer("Commentaire");
    }

    // GET: api/commentaires
    [HttpGet]
    public async Task<IActionResult> GetAllCommentaires()
    {
        var query = _container.GetItemQueryIterator<Commentaire>(new QueryDefinition("SELECT * FROM c"));
        var results = new List<Commentaire>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return Ok(results);
    }

    // GET: api/commentaires/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCommentaireById(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Commentaire>(id, new PartitionKey(id));
            return Ok(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Commentaire introuvable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur interne : {ex.Message}");
            return StatusCode(500, "Erreur interne du serveur.");
        }
    }

    // POST: api/commentaires
    [HttpPost]
    public async Task<IActionResult> CreateCommentaire([FromBody] Commentaire commentaire)
    {
        try
        {
            if (commentaire == null)
            {
                return BadRequest("Le corps de la requ�te est vide.");
            }

            if (string.IsNullOrWhiteSpace(commentaire.Titre) || string.IsNullOrWhiteSpace(commentaire.Contenu))
            {
                return BadRequest("Les champs 'Titre' et 'Contenu' sont obligatoires.");
            }

            // Cr�e un nouveau commentaire avec un ID unique
            Commentaire document = new Commentaire
            {
                Titre = commentaire.Titre,
                Contenu = commentaire.Contenu,
                DateCreation = DateTime.UtcNow,
                id = Guid.NewGuid().ToString()
            };

            await _container.CreateItemAsync(document, new PartitionKey(document.id));

            return CreatedAtAction(nameof(GetCommentaireById), new { id = document.id }, document);
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

    // PUT: api/commentaires/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommentaire(string id, [FromBody] Commentaire commentaire)
    {
        try
        {
            if (commentaire == null)
            {
                return BadRequest("Le corps de la requ�te est vide.");
            }

            var existingItem = await _container.ReadItemAsync<Commentaire>(id, new PartitionKey(id));
            if (existingItem == null)
            {
                return NotFound("Commentaire introuvable.");
            }

            // Mettre � jour les champs du commentaire
            existingItem.Resource.Titre = commentaire.Titre ?? existingItem.Resource.Titre;
            existingItem.Resource.Contenu = commentaire.Contenu ?? existingItem.Resource.Contenu;

            // Mettre � jour l'�l�ment dans Cosmos DB
            await _container.ReplaceItemAsync(existingItem.Resource, id, new PartitionKey(id));

            return Ok(existingItem.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Commentaire introuvable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur interne : {ex.Message}");
            return StatusCode(500, "Erreur interne du serveur.");
        }
    }

    // DELETE: api/commentaires/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommentaire(string id)
    {
        try
        {
            await _container.DeleteItemAsync<Commentaire>(id, new PartitionKey(id));
            return NoContent(); // Retourne un statut 204 No Content
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound("Commentaire introuvable.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur interne : {ex.Message}");
            return StatusCode(500, "Erreur interne du serveur.");
        }
    }
}

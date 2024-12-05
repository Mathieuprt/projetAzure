using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SocialMediaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly BlobContainerClient _blobContainer;

    public MediaController(BlobServiceClient blobServiceClient)
    {
        // Cr�er ou obtenir le conteneur "media"
        _blobContainer = blobServiceClient.GetBlobContainerClient("media");
        _blobContainer.CreateIfNotExists(PublicAccessType.Blob);
    }

    /// <summary>
    /// T�l�charge un fichier vers Azure Blob Storage.
    /// </summary>
    /// <param name="file">Fichier � t�l�charger</param>
    /// <returns>URL du fichier t�l�charg�</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("Le fichier est vide.");
        }

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.File.FileName);
        var blobClient = _blobContainer.GetBlobClient(fileName);

        using (var stream = request.File.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        return Ok(new { Url = blobClient.Uri.ToString(), FileName = fileName });
    }


    /// <summary>
    /// Récupère un fichier depuis Azure Blob Storage.
    /// </summary>
    /// <param name="fileName">Nom du fichier à récupérer</param>
    /// <returns>Le fichier téléchargé</returns>
    [HttpGet("{fileName}")]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        try
        {
            // V�rifier si le fichier existe
            var blobClient = _blobContainer.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return NotFound("Fichier non trouv�.");

            // R�cup�rer le contenu du fichier
            var stream = await blobClient.OpenReadAsync();
            var contentType = (await blobClient.GetPropertiesAsync()).Value.ContentType ?? "application/octet-stream";

            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la r�cup�ration du fichier : {ex.Message}");
        }
    }

    /// <summary>
    /// Liste tous les fichiers disponibles dans le conteneur.
    /// </summary>
    /// <returns>Liste des noms de fichiers</returns>
    [HttpGet("list")]
    public async Task<IActionResult> ListFiles()
    {
        try
        {
            var blobs = _blobContainer.GetBlobsAsync();
            var files = new List<string>();

            await foreach (var blob in blobs)
            {
                files.Add(blob.Name);
            }

            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la r�cup�ration de la liste des fichiers : {ex.Message}");
        }
    }

    /// <summary>
    /// Supprime un fichier du conteneur Azure Blob Storage.
    /// </summary>
    /// <param name="fileName">Nom du fichier � supprimer</param>
    /// <returns>Statut de suppression</returns>
    [HttpDelete("{fileName}")]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        try
        {
            // V�rifier si le fichier existe
            var blobClient = _blobContainer.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return NotFound("Fichier non trouv�.");

            // Supprimer le fichier
            await blobClient.DeleteAsync();

            return Ok("Fichier supprim� avec succ�s.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erreur lors de la suppression du fichier : {ex.Message}");
        }
    }
}

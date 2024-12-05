namespace SocialMediaApi.Models;

public class Publication
{
    public string id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public string Media { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }

    public Publication()
    {
        id  = Guid.NewGuid().ToString();
    }
}



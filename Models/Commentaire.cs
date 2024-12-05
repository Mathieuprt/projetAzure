public class Commentaire
{
    public string id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Commentaire()
    {
        id = Guid.NewGuid().ToString();
    }
}

public class Utilisateur
{
    public string IdUtilisateur { get; set; } = Guid.NewGuid().ToString();
    public string NomUtilisateur { get; set; } = string.Empty; 
    public string Email { get; set; } = string.Empty; 
    public string MotDePasse { get; set; } = string.Empty; 
    public bool ProfilPublic { get; set; } = true; 
}

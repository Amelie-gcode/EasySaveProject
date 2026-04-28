namespace EasySave.Models
{
    /// <summary>
    /// Objet de transfert de données pour la configuration des jobs.
    /// </summary>
    public class JobSaveData
    {
        public string Name { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public bool IsDifferential { get; set; }
    }
}
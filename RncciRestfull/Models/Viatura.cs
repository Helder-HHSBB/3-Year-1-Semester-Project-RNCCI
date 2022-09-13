namespace RncciRestfull.Models
{
    public class Viatura
    {
        public int Cod_viatura { get; set; }
        public int Cod_unidade_movel { get; set; }
        public string Matricula { get; set; }
        public string Marca { get; set; }
        public bool Estado_disponibilidade { get; set; }
    }
}

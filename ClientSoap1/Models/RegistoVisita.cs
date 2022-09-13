using System;

namespace ClientSoap1.Models
{
    class RegistoVisita
    {
        public int Cod_registo_visita { get; set; }

        public int Cod_utente { get; set; }

        public int Cod_visitante { get; set; }

        public int Cod_unidadeFisica { get; set; }

        public DateTime Registo_entrada { get; set; }

        public DateTime Registo_saida { get; set; }
    }
}

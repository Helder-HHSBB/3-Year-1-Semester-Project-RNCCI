using System;

namespace RncciRestfull.Models
{
    public class RegistoOcupacao
    {

        public int Cod_ocupacao { get; set; }


        public int Cod_utente { get; set; }


        public int Cod_cama { get; set; }


        public int Cod_unidade_movel { get; set; }


        public DateTime Data_inicio { get; set; }


        public DateTime Data_fim { get; set; }
    }
}

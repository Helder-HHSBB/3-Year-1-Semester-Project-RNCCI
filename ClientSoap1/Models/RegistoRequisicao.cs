using System;

namespace ClientSoap1.Models
{
    class RegistoRequisicao
    {
        public int Cod_requisicao { get; set; }


        public int Cod_profissional { get; set; }


        public int Cod_viatura { get; set; }


        public DateTime Data_pedido { get; set; }

        public bool Aceite { get; set; }

        public DateTime Data_inicio_uso { get; set; }

        public DateTime Data_fim_uso { get; set; }
    }
}

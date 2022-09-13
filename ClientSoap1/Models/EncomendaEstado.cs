using System;

namespace ClientSoap1.Models
{
    class EncomendaEstado
    {
        public int Cod_encomenda { get; set; }

        public int Cod_unidade_fisica { get; set; }

        public DateTime Data_pedido { get; set; }

        public bool Estado { get; set; }
    }
}

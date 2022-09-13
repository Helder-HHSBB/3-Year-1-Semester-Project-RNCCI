using System;

namespace CentralComprasRestfull.Models
{
    public class EncomendaEstado
    {
        public int Cod_encomenda { get; set; }
        public int Cod_unidade_fisica { get; set; }
        public DateTime Data_pedido { get; set; }
        public bool Estado { get; set; }
    }
}

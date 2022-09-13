using System;

namespace ClientSoap1.Models
{
    class ViaturasDisponiveis : Viatura
    {
        public DateTime Hora_inicio_uso { get; set; }

        public DateTime Hora_fim_uso { get; set; }
    }
}

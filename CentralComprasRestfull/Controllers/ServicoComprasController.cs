using CentralComprasRestfull.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;

namespace CentralComprasRestfull.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicoComprasController : Controller
    {
        // POST: Uma nova encomenda na base de dados.
        [HttpPost("/Encomenda")]
        [ProducesResponseType(201)]
        public IActionResult PostEncomendas([FromBody] EncomendaEstado encomendaEstado)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();
                DateTime todaysDate = DateTime.Now;
                string sql = ($"Insert into encomenda_estado (cod_unidade_fisica, data_pedido, estado) values ({encomendaEstado.Cod_unidade_fisica},'{todaysDate.Day}/{todaysDate.Month}/{todaysDate.Year} {todaysDate.Hour}:{todaysDate.Minute}:{todaysDate.Second}', false) returning cod_encomenda");
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                int codEncomenda = rdr.GetInt32(0);
                return CreatedAtAction("GetEncomenda", codEncomenda);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: Produtos, numa encomenda. 
        [HttpPost("/Produtos")]
        [ProducesResponseType(201)]
        public IActionResult PostProdutos([FromBody] List<EncomendaProduto> encomendaProdutos)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();
                foreach (EncomendaProduto encomendaProduto in encomendaProdutos)
                {
                    string sql = ($"Insert into encomenda_produto (cod_encomenda, cod_produto, quantidade) values ({encomendaProduto.Cod_encomenda}, {encomendaProduto.Cod_produto}, {encomendaProduto.Quantidade})");
                    var cmd = new NpgsqlCommand(sql, con);
                    var insert = cmd.ExecuteNonQuery();
                }
                con.Close();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //Get Encomenda
        [HttpGet]
        [ActionName("GetEncomenda")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<EncomendaEstado> GetEncomenda(int codEncomenda)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();
                string sql = ($"select * from encomenda_estado where cod_encomenda = {codEncomenda}");
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                EncomendaEstado encomenda = (new EncomendaEstado { Cod_encomenda = rdr.GetInt32(0), Data_pedido = rdr.GetDateTime(1), Estado = rdr.GetBoolean(2), Cod_unidade_fisica = rdr.GetInt32(3) });
                con.Close();

                if (encomenda == null)
                    return NotFound("não existe encomenda com este codigo");

                return Ok(encomenda);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}


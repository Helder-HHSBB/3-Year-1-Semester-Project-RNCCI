using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RncciRestfull.Models;
using System;
using System.Collections.Generic;

namespace RncciRestfull.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistoVisitasController : Controller
    {
        //GET utente através do código de utente
        [HttpGet("/Utente/{codUtente}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<Utente> GetUtente(int codUtente)
        {

            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();

                string sql = ($"select * from utente where cod_utente = {codUtente}");
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                Utente utente = (new Utente { Cod_utente = rdr.GetInt32(0), Data_nascimeto = rdr.GetDateTime(1), Nome = rdr.GetString(2), Morada = rdr.GetString(3) });
                con.Close();
                
                return Ok(utente);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        //Get visitante através do código de visitante
        [HttpGet("/Visitante/{codVisitante}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<Visitante> GetVisitante(int codVisitante)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();
                string sql = ($"select * from visitante where cod_visitante = {codVisitante}");
                var cmd = new NpgsqlCommand(sql, con);
                
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                Visitante visitante = (new Visitante { Cod_visitante = rdr.GetInt32(0), Nome = rdr.GetString(1) });
                con.Close();
                return Ok(visitante);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

       
        [HttpGet("/VisitaPorFechar/{codVisitante}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        //Procura as visitas que este visitante tem em aberto, e devolve a lista das mesmas. (um visitante pode visitar 1 ou mais doentes na mesma unidade ao mesmo tempo)
        public ActionResult<List<RegistoVisita>> GetVisitasPorFecharVisitante(int codVisitante)
        {
            try
            {
                List<RegistoVisita> registosVisita = new();
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();

                //Se o registo de saida dele for ano 1900-01-01 00:00:00 significa que ainda não lhe foi atribuida uma saida. 
                string sql = ($"select * from registo_visita where cod_visitante = {codVisitante} and registo_saida = '1900-01-01 00:00:00'");
                var cmd = new NpgsqlCommand(sql, con);
               
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    registosVisita.Add(new RegistoVisita { Cod_registo_visita = rdr.GetInt32(0), Cod_utente = rdr.GetInt32(1), Cod_visitante = rdr.GetInt32(2), Registo_entrada = rdr.GetDateTime(3), Registo_saida = rdr.GetDateTime(4), Cod_unidadeFisica = rdr.GetInt32(5) });
                }
                con.Close();
                return Ok(registosVisita);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        //Verifica se este doente está atualmente internado numa cama de uma unidade fisica..
        [HttpPost("/UtenteInternado")]
        [ProducesResponseType(201)]
        public ActionResult<bool> VerificaUtenteIternadoNaRede(Internado internado)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();

                string sql = ($"select * from public.registo_ocupacao as ro inner join cama on cama.cod_Cama = ro.cod_cama inner join unidade_saude_fisica as usf on usf.cod_unidade_fisica = cama.cod_unidade_fisica " +
                              $"where cod_utente = {internado.CodUtente} and estado = false and cama.cod_unidade_fisica = {internado.CodUnidade} ");
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                
                if (rdr.HasRows)
                {
                    Internado registoInternado = (new Internado { CodUtente = rdr.GetInt32(1), CodUnidade = rdr.GetInt32(7) });
                    
                    if (internado.CodUtente == registoInternado.CodUtente & internado.CodUnidade == registoInternado.CodUnidade)
                    {
                        con.Close();
                        return true;
                    }
                    else 
                    {
                        con.Close();
                        return false;
                    }
                        

                }
                con.Close();
                return false;
            }
            catch 
            {
                return (false);
            }
            
        }

        //Get unidade fisica através do seu código
        [HttpGet("/UnidadeFisica/{codUnidadeFisica}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<Visitante> GetUnidadeFisica(int codUnidadeFisica)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();

                string sql = ($"select * from unidade_saude_fisica where cod_unidade_fisica = {codUnidadeFisica}");
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                UnidadeSaudeFisica unidadeFisica = (new UnidadeSaudeFisica { Cod_unidade_fisica = rdr.GetInt32(0), Tipologia = rdr.GetInt32(1), Nome = rdr.GetString(2), Regiao = rdr.GetString(3) });
                con.Close();
                
                return Ok(unidadeFisica);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }


        // POST: Insere um novo registo de visitas válido
        [HttpPost]
        [ProducesResponseType(201)]
        public IActionResult Post([FromBody] RegistoVisita registoVisita)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();
                DateTime todaysDate = DateTime.Now;
                
                //usamos a validação de ano == 1900 para identificar na tabela, que esta visita ainda nao tem uma hora de saida. 
                //Se for diferente de 1900 inserimos uma nova visita, se for == sabemos que apenas queremos fazer um update na hora de saida (que será a hora do sistema no momento do request)
                if (registoVisita.Registo_saida.Year != 1900)
                {

                    string sql = ($"Insert into registo_visita (cod_utente, cod_visitante, cod_unidade_fisica, registo_entrada, registo_saida) values ({registoVisita.Cod_utente},{registoVisita.Cod_visitante},{registoVisita.Cod_unidadeFisica}," +
                                    $"'{todaysDate.Day}/{todaysDate.Month}/{todaysDate.Year} {todaysDate.Hour}:{todaysDate.Minute}:{todaysDate.Second}','01/01/1900 00:00:00')");
                    var cmd = new NpgsqlCommand(sql, con);
                    var insert = cmd.ExecuteNonQuery();
                    con.Close();
                    
                    return Ok();
                }
                else //a visita existe mas não saiu.
                {
                    string sql = ($"Update registo_visita set registo_saida = '{todaysDate.Day}/{todaysDate.Month}/{todaysDate.Year} {todaysDate.Hour}:{todaysDate.Minute}:{todaysDate.Second}' where cod_registo_visita = {registoVisita.Cod_registo_visita}");
                    var cmd = new NpgsqlCommand(sql, con);
                    var insert = cmd.ExecuteNonQuery();
                    registoVisita.Registo_saida = todaysDate;
                    con.Close();
                    
                    return Ok(registoVisita);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

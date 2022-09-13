using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RncciRestfull.Models;
using System;
using System.Collections.Generic;


namespace RncciRestfull.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistoViaturasController : Controller
    {
        //Get uma viatura através do codigo 
        [HttpGet("/Viatura/{codViatura}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<Viatura> GetViatura(int codViatura)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();

                string sql = ($"select * from viatura where cod_viatura = {codViatura}");
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                
                rdr.Read();
                Viatura viatura = (new Viatura { Cod_viatura = rdr.GetInt32(0), Cod_unidade_movel = rdr.GetInt32(1), Matricula = rdr.GetString(2), Marca = rdr.GetString(3), Estado_disponibilidade = rdr.GetBoolean(4) });
                
                return Ok(viatura);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        //Get Autentica profissional de saude e devolve o objecto profissional saude.
        [HttpGet("/Profissional/{codProfissional}/{nrCedula}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        
        public ActionResult<ProfissionalSaude> GetProfissional(int codProfissional, int nrCedula)
        {
            try
            {
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();
                string sql = ($"select * from profissional_saude where cod_profissional = {codProfissional} and nr_cedula = {nrCedula}");
                
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                ProfissionalSaude profissionalSaude = (new ProfissionalSaude { Cod_profissional = rdr.GetInt32(0), Nr_cedula = rdr.GetInt32(1), Nome = rdr.GetString(2), Cod_unidade_Movel = rdr.GetInt32(3) });
                
                con.Close();
                return Ok(profissionalSaude);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        //GET viaturas disponiveis numa região, na data indicada
        [HttpGet("/Viaturas/{regiao}/{sDataInicio}/{sDataFim}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<List<Viatura>> GetViaturasDisponiveis(string regiao, string sDataInicio, string sDataFim)
        {
            try
            {
                List<Viatura> viaturas = new();
                //Como recebe os parametros por url, tivemos de formatar as strings para evitar perdas de informação. 
                List<int> codViaturasOcupadasNessaData = new();
                sDataInicio = sDataInicio.Replace('~', '/').Replace('-', ' ');
                DateTime dataInicio = Convert.ToDateTime(sDataInicio);
                sDataFim = sDataFim.Replace('~', '/').Replace('-', ' ');
                DateTime dataFim = Convert.ToDateTime(sDataFim);

                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                
                //Seleciona as viaturas disponiveis na base de dados para a regiao indicada
                con.Open();
                regiao = regiao.ToUpper();
                string sql = ($"select * from public.viatura as v inner join public.unidade_movel as um on v.cod_unidade_movel = um.cod_unidade_movel where um.regiao = '{regiao}' and v.estado_disponibilidade = true ");
                var cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr = cmd.ExecuteReader();
                
                //carrega as viaturas disponiveis para uma lista
                while (rdr.Read())
                {
                    viaturas.Add(new Viatura { Cod_viatura = rdr.GetInt32(0), Cod_unidade_movel = rdr.GetInt32(1), Matricula = rdr.GetString(2), Marca = rdr.GetString(3), Estado_disponibilidade = rdr.GetBoolean(4) });
                }
                con.Close();

                //Seleciona da base de dados, as viaturas indisponiveis entre as datas indicadas para a requisição de uma viatura.

                con.Open();
                sql = ($"select * from public.registo_requisicao where ('{dataInicio.Day}/{dataInicio.Month}/{dataInicio.Year} {dataInicio.Hour}:{dataInicio.Minute}:{dataInicio.Second}' " +
                        $"between data_inicio_uso and data_fim_uso) AND ('{dataFim.Day}/{dataFim.Month}/{dataFim.Year} {dataFim.Hour}:{dataFim.Minute}:{dataFim.Second}' between data_inicio_uso and data_fim_uso)");
                cmd = new NpgsqlCommand(sql, con);
                NpgsqlDataReader rdr2 = cmd.ExecuteReader();
                if (rdr2.HasRows)
                {
                    while (rdr2.Read())
                    {
                        codViaturasOcupadasNessaData.Add(rdr2.GetInt32(2));
                    }
                    if (codViaturasOcupadasNessaData != null)
                    {
                        //criamos lista de viaturasDispooniveis auxiliar para comparar com a lista de viaturas ocupadas na data requisitada, e eliminar as que se encontram Disponiveis na Regiao, mas ocupadas nessa data (remove repetidas).
                        List<Viatura> viaturasDisponiveis = new(viaturas);
                        foreach (Viatura viatura in viaturas)
                        {
                            foreach (int codViaturaOcupada in codViaturasOcupadasNessaData)
                            {
                                if (viatura.Cod_viatura.Equals(codViaturaOcupada))
                                {
                                    viaturasDisponiveis.Remove(viatura);
                                }
                            }
                        }
                        //se nao houver viaturas ocupadas nessa data, por defeito retorna só as disponiveis na regiao
                        return Ok(viaturasDisponiveis);
                    }
                }
                return Ok(viaturas);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // POST: Regista a requisição de uma viatura feita pelo profissional de saude. 
        [HttpPost]
        [ProducesResponseType(201)]
        public IActionResult PostRegistoRequisicao([FromBody] RegistoRequisicao registoRequisicao)
        {
            try
            {
                //Insere Registo de Requisicao de viatura
                NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                con.Open();
                string sql = ($"insert into registo_requisicao (cod_profissional, cod_viatura, data_pedido, aceite, data_inicio_uso, data_fim_uso) values({registoRequisicao.Cod_profissional}, {registoRequisicao.Cod_viatura}, " +
                              $"'{registoRequisicao.Data_pedido.Day}/{registoRequisicao.Data_pedido.Month}/{registoRequisicao.Data_pedido.Year} {registoRequisicao.Data_pedido.Hour}:{registoRequisicao.Data_pedido.Minute}:{registoRequisicao.Data_pedido.Second}', " +
                              $"false, '{registoRequisicao.Data_inicio_uso.Day}/{registoRequisicao.Data_inicio_uso.Month}/{registoRequisicao.Data_inicio_uso.Year} {registoRequisicao.Data_inicio_uso.Hour}:{registoRequisicao.Data_inicio_uso.Minute}:{registoRequisicao.Data_inicio_uso.Second}', " +
                              $"'{registoRequisicao.Data_fim_uso.Day}/{registoRequisicao.Data_fim_uso.Month}/{registoRequisicao.Data_fim_uso.Year} {registoRequisicao.Data_fim_uso.Hour}:{registoRequisicao.Data_fim_uso.Minute}:{registoRequisicao.Data_fim_uso.Second}')");
                var cmd = new NpgsqlCommand(sql, con);
                var insert = cmd.ExecuteNonQuery();
                con.Close();
                
                //Após registo update disponibilidade de viatura para falso. 
                con.Open();
                sql = ($"Update viatura set estado_disponibilidade = false where viatura.cod_viatura = {registoRequisicao.Cod_viatura}");
                cmd = new NpgsqlCommand(sql, con);
                insert = cmd.ExecuteNonQuery();
                con.Close();
                
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //Atualiza estado das viaturas. 
        [HttpPut]
        [ProducesResponseType(201)]
        public IActionResult PutAtulizaViaturasDisponiveis([FromBody] bool pedido)
        {
            try
            {
                if (pedido == true)
                {
                    NpgsqlConnection con = new("Server=localhost;Port=5432;User Id=postgres;Password=rust; Database=rncci_db;");
                    con.Open();
                    string sql = ($"update public.viatura v set estado_disponibilidade = true FROM registo_requisicao where v.cod_viatura = registo_requisicao.cod_viatura and registo_requisicao.data_fim_uso < localtimestamp");
                    var cmd = new NpgsqlCommand(sql, con);
                    var insert = cmd.ExecuteNonQuery();
                    con.Close();
                    return Ok(true);
                }
                return BadRequest(false);
            }
            catch
            {
                return BadRequest(false);
            }
        }

    }
}





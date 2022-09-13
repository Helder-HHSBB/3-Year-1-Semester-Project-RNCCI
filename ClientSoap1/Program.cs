using ClientSoap1.CamasService;
using ClientSoap1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ClientSoap1
{
    class Program
    {
        //Preparação de host portas a usar nos serviços e endereço e HttpClient
        private static readonly string host = "localhost";
        private static readonly int portRest = 44336;
        private static readonly int portEncomendasEstado = 44342;
        private static readonly string mediaType = "application/json";
        private static readonly HttpClient clientRest = new HttpClient();
        static void Main()
        {
            // Preparação do client rest.
            clientRest.DefaultRequestHeaders.Accept.Clear();
            clientRest.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));

            int op = 9;
            while (op != 0)
            {
                Console.Clear();
                Console.WriteLine(" Bem-Vindo IPCA RNCCI Gestao de Camas ");
                Console.WriteLine();
                Console.WriteLine(" 1- Atribui cama a um novo doente na rede consoante disponibilidade.  ");
                Console.WriteLine(" 2- Registo de entrada de um visitante. ");
                Console.WriteLine(" 3- Registar a saida de um visitante. ");
                Console.WriteLine(" 4- Autenticar Enfermeiro e Requisitar uma viatura para data e região disponivel. ");
                Console.WriteLine(" 5- Encomendar Material para Unidade Fisica à central de compras do estado. ");
                Console.WriteLine(" 0- Exit Application");
                op = Convert.ToInt32(Console.ReadLine());
                switch (op)
                {
                    case 1:
                        {
                            AtribuirCamaUtente();
                        }
                        break;

                    case 2:
                        {
                            UnidadeSaudeFisica unidadeSaudeFisica = GetUnidadeFisica();
                            Visitante visitante = GetVisitante();
                            Utente utente = GetUtente();
                            bool estaInternado = VerificaUtenteIternadoNaRede(utente, unidadeSaudeFisica);
                            if (estaInternado == true)
                            {
                                RegistoVisitaEntrada(utente, visitante, unidadeSaudeFisica);
                                Console.WriteLine($"Visita de {visitante.Nome} a {utente.Nome} na Unidade de {unidadeSaudeFisica.Nome} registada em BD. ");
                                Console.ReadLine();
                            }
                            else
                            {
                                Console.WriteLine("Este utente não está internado, ou não pertence a esta Unidade.");
                                Console.WriteLine("Abortar...");
                                Console.ReadKey();
                            }
                        }
                        break;

                    case 3:
                        {
                            Visitante visitante = GetVisitante();
                            GetVisitasPorFecharVisitante(visitante.Cod_visitante);
                        }
                        break;

                    case 4:
                        {
                            RegistoPedidoRequisicao();
                        }
                        break;

                    case 5:
                        {
                            CriaEncomenda();
                        }
                        break;
                }
            }

            #region Soap Methods Atribuir Cama
            
            //Método para atribuir cama a utente usando serviço soap.
            void AtribuirCamaUtente()
            {
                try
                {
                    AtribuiCamaClient client = new AtribuiCamaClient();
                    List<Cama> camas;

                    Console.Clear();
                    Console.WriteLine("Codigo de Unidade Fisica a Listar Camas Disponiveis: ");

                    int codUnidade = Convert.ToInt32(Console.ReadLine());

                    //invoca metodo GetCamaDisponivel para a unidade indicada, e guarda na lista camas.
                    camas = client.GetCamaDisponivel(codUnidade);

                    if (camas.Count > 0)
                    {
                        foreach (Cama cama in camas)
                        {
                            Console.WriteLine($"Codigo Cama: {cama.Cod_cama} Status: Disponivel");
                        }
                        Console.WriteLine("");
                        Console.WriteLine("Deseja adicionar um utente a uma cama? sim: 1 nao: 0");
                        int aux = Convert.ToInt32(Console.ReadLine());

                        if (aux == 1)
                        {
                            Console.Write("Indique Codigo Utente: ");
                            int codUtente = Convert.ToInt32(Console.ReadLine());
                            Console.Write("Indique Codigo da Cama: ");
                            int codCama = Convert.ToInt32(Console.ReadLine());
                            Cama cama = camas.FirstOrDefault(c => c.Cod_cama.Equals(codCama));

                            if (cama != null)
                            {
                                DateTime dataFim = GetDateFromUser("Insira a data de alta para este utente: ");

                                while (dataFim < DateTime.Now)
                                {
                                    Console.WriteLine("A data final não pode ser inferior a data de inicio");
                                    dataFim = GetDateFromUser("Insira a data de alta para este utente: ");
                                }

                                    var result = client.AtribuiCamaUtente(codCama, codUtente, dataFim);
                                if (result == true)
                                {
                                    Console.WriteLine("Adicionado com Sucesso");
                                    Console.ReadKey();
                                }
                                if (result == false)
                                {
                                    Console.WriteLine("Inseriu um utente que está atualmente internado.");
                                    Console.ReadKey();
                                    return;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Inseriu uma cama inválida.");
                                Console.ReadKey();
                                return;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Esta unidade não tem camas disponiveis");
                        Console.ReadKey();
                    }
                    client.Close();
                }
                catch 
                {
                    return ;
                }
            }
            #endregion

            #region Rest Methods Registo Visitas
            
            //Get unidade fisica indicada pelo user
            UnidadeSaudeFisica GetUnidadeFisica()
            {
                string requestUri;
                UnidadeSaudeFisica unidadeSaudeFisica;
                HttpResponseMessage response;

                Console.WriteLine("Informa a Unidade de saude fisica que pretende: ");
                int codUnidadeFisica = Convert.ToInt32(Console.ReadLine());
                
                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/UnidadeFisica/{codUnidadeFisica}";

                try
                {
                    response = clientRest.GetAsync(requestUri).Result;
                    if (!response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        throw new Exception(response.Content.ReadAsStringAsync().Result);
                    }

                    unidadeSaudeFisica = response.Content.ReadAsAsync<UnidadeSaudeFisica>().Result;
                    if (unidadeSaudeFisica != null)
                    {
                        Console.WriteLine($"Selecionada Unidade com Codigo: {unidadeSaudeFisica.Cod_unidade_fisica}, Nome: {unidadeSaudeFisica.Nome},Regiao: {unidadeSaudeFisica.Regiao}");
                        return (unidadeSaudeFisica);

                    }
                    else
                    {
                        Console.WriteLine("Este codigo Unidade Saude Fisica não está registado na BD");
                        Console.ReadKey();
                        return (unidadeSaudeFisica);
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            }

            //Método para usar o serviço rest para obter um utente.
            Utente GetUtente()
            {
                string requestUri;
                Utente utente;
                HttpResponseMessage response;

                Console.WriteLine("Informe codigo de  Utente que pretende: ");
                int codUtente = Convert.ToInt32(Console.ReadLine());

                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/Utente/{codUtente}";

                try
                {
                    response = clientRest.GetAsync(requestUri).Result;
                    if (!response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        throw new Exception(response.Content.ReadAsStringAsync().Result);
                    }
                    utente = response.Content.ReadAsAsync<Utente>().Result;
                    if (utente != null)
                    {

                        Console.WriteLine($"Seleccionado Utente com Nome: {utente.Nome} Codigo: {utente.Cod_utente} Data Nascimento: {utente.Data_nascimeto} Morada: {utente.Morada}");
                        return (utente);
                    }
                    else
                    {
                        Console.WriteLine("Este codigo de Utente não está registado na BD");
                        Console.ReadKey();
                        return (utente);
                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            }

            //Método utilizado com o serviço rest para obter um visitante
            Visitante GetVisitante()
            {
                string requestUri;
                Visitante visitante;
                HttpResponseMessage response;

                Console.WriteLine("Informe o Visitante que pretende: ");
                int codVisitante = Convert.ToInt32(Console.ReadLine());
                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/Visitante/{codVisitante}";

                try
                {
                    response = clientRest.GetAsync(requestUri).Result;
                    if (!response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        throw new Exception(response.Content.ReadAsStringAsync().Result);
                    }

                    visitante = response.Content.ReadAsAsync<Visitante>().Result;
                    if (visitante != null)
                    {
                        Console.WriteLine($"Seleccionado Visitante com Nome: {visitante.Nome} e Codigo: {visitante.Cod_visitante}");
                        return (visitante);

                    }
                    else
                    {
                        Console.WriteLine("Este codigo de visitante não está registado na BD");
                        Console.ReadKey();
                        return (visitante);
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            }

            //Método para registar uma visita de um visitante a um utente numa unidade fisica, REST
            void RegistoVisitaEntrada(Utente utente, Visitante visitante, UnidadeSaudeFisica unidadeSaudeFisica)
            {
                string requestUri;
                RegistoVisita registoVisita;
                HttpResponseMessage response;
                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/api/RegistoVisitas";

                try
                {
                    DateTime dateNow = DateTime.Now;


                    response = clientRest.PostAsJsonAsync<RegistoVisita>(requestUri, new RegistoVisita
                    {
                        Cod_utente = utente.Cod_utente,
                        Cod_visitante = visitante.Cod_visitante,
                        Registo_entrada = dateNow,
                        Cod_unidadeFisica = unidadeSaudeFisica.Cod_unidade_fisica
                    }).Result;
                    
                    //confirmação da inserção de um registo de visita
                    registoVisita = response.Content.ReadAsAsync<RegistoVisita>().Result;
                    var location = response.Headers.Location;
                    Console.WriteLine($"{visitante.Nome} Visita : {utente.Nome} na : {unidadeSaudeFisica.Cod_unidade_fisica}, {unidadeSaudeFisica.Nome} às : {dateNow.Hour}:{dateNow.Minute}:{dateNow.Second}!");
                    Console.WriteLine(location);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return;
                }
            }

            //Fecha um registo de visita, rest, com a data atual do sistema. 
            void FechaRegistoVisita(RegistoVisita registoUpdate)
            {
                string requestUri;
                RegistoVisita registoVisita;
                HttpResponseMessage response;
                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/api/RegistoVisitas";

                try
                {
                    response = clientRest.PostAsJsonAsync<RegistoVisita>(requestUri, registoUpdate).Result;
      
                    registoVisita = response.Content.ReadAsAsync<RegistoVisita>().Result;
                    var location = response.Headers.Location;

                    Console.WriteLine($"Registo visita encerrado, Codigo {registoVisita.Cod_registo_visita} Visitante {registoVisita.Cod_visitante} visita {registoVisita.Cod_utente} na Unidade {registoVisita.Cod_unidadeFisica}" +
                                        $" Entrada: {registoVisita.Registo_entrada.Day}/{registoVisita.Registo_entrada.Month}/{registoVisita.Registo_entrada.Year} {registoVisita.Registo_entrada.Hour}:{registoVisita.Registo_entrada.Minute}:{registoVisita.Registo_entrada.Second}" +
                                        $"Saida: {registoVisita.Registo_saida.Day}/{registoVisita.Registo_saida.Month}/{registoVisita.Registo_saida.Year} {registoVisita.Registo_saida.Hour}:{registoVisita.Registo_saida.Minute}:{registoVisita.Registo_saida.Second}");
                    Console.WriteLine(location);
                    Console.ReadLine();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }

            }

            //get as visitas por fechar, em que o ano sao 1900. 
            bool GetVisitasPorFecharVisitante(int codVisitante)
            {
                String requestUri;
                List<RegistoVisita> registosVisita = new List<RegistoVisita>();
                HttpResponseMessage response;

                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/VisitaPorFechar/{codVisitante}";

                try
                {
                    response = clientRest.GetAsync(requestUri).Result;
                    if (!response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        throw new Exception(response.Content.ReadAsStringAsync().Result);
                    }

                    registosVisita = response.Content.ReadAsAsync<List<RegistoVisita>>().Result;
                    if (registosVisita.Count() > 0)
                    {
                        foreach (RegistoVisita registoAFechar in registosVisita)
                        {
                            Console.WriteLine($"Codigo Registo : {registoAFechar.Cod_registo_visita} | Hora Entrada: {registoAFechar.Registo_entrada} Visitante: {registoAFechar.Cod_visitante}");
                        }
                        
                        Console.WriteLine("Indique o Codigo Registo a fechar:");
                        int codRegisto = Convert.ToInt32(Console.ReadLine());
                        RegistoVisita registoUpdate = registosVisita.FirstOrDefault(r => r.Cod_registo_visita.Equals(codRegisto));
                        
                        //Invoca metodo rest para fechar o registo de visita ---> realiza update na hora de saida com hora do sistema.
                        FechaRegistoVisita(registoUpdate);

                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Este codigo de visitante não tem nenhuma visita em aberto");
                        Console.ReadKey();
                        return false;
                    }
                }


                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return false;
                }
            }

            #endregion

            #region Rest Methods Requisicao Viaturas
            
            //Serviço Rest para obter viaturas disponiveis, considerando regiao e datas. 
            List<ViaturasDisponiveis> GetViaturasDisponiveis()
            {
                string requestUri;
                List<ViaturasDisponiveis> viaturasDisponiveis = new List<ViaturasDisponiveis>();
                List<Viatura> viaturas = new List<Viatura>();
                HttpResponseMessage response;

                //Como envia os parametros por url, tivemos de formatar as strings para evitar corrupçao de informação. 
                DateTime dataInicio = GetDateFromUser("Insira Data Inicio: ");
                string sDataInicio = dataInicio.ToString();
                sDataInicio = sDataInicio.Replace('/', '~').Replace(' ', '-');
                DateTime dataFim = GetDateFromUser("Insira Data Fim: ");
                string sDataFim = dataFim.ToString();
                sDataFim = sDataFim.Replace('/', '~').Replace(' ', '-');

                while (dataFim < dataInicio || dataInicio < DateTime.Now)
                {
                    if (dataFim < dataInicio)
                    {
                        Console.WriteLine("A data final não pode ser inferior a data de inicio");
                    }
                    if (dataInicio < DateTime.Now)
                    {
                        Console.WriteLine("A data incial não pode ser inferior ao momento atual");
                    }
                    
                    dataInicio = GetDateFromUser("Insira Data Inicio: ");
                    sDataInicio = dataInicio.ToString();
                    sDataInicio = sDataInicio.Replace('/', '~').Replace(' ', '-');
                    
                    dataFim = GetDateFromUser("Insira Data Fim: ");
                    sDataFim = dataFim.ToString();
                    sDataFim = sDataFim.Replace('/', '~').Replace(' ', '-');
                }

                Console.WriteLine("Para que regiao quer a viatura: ");
                string regiao = Convert.ToString(Console.ReadLine());
                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/Viaturas/{regiao}/{sDataInicio}/{sDataFim}";

                try
                {
                    response = clientRest.GetAsync(requestUri).Result;
                    if (!response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        throw new Exception(response.Content.ReadAsStringAsync().Result);
                    }

                    viaturas = response.Content.ReadAsAsync<List<Viatura>>().Result;
                    if (viaturas != null)
                    {
                        Console.WriteLine("Lista de Viaturas disponivel: ");
                        foreach (Viatura viatura in viaturas)
                        {
                            viaturasDisponiveis.Add(new ViaturasDisponiveis
                            {
                                Cod_viatura = viatura.Cod_viatura,
                                Cod_unidade_movel = viatura.Cod_unidade_movel,
                                Marca = viatura.Marca,
                                Matricula = viatura.Matricula,
                                Estado_disponibilidade = viatura.Estado_disponibilidade,
                                Hora_inicio_uso = dataInicio,
                                Hora_fim_uso = dataFim
                            });
                        }
                        return (viaturasDisponiveis);
                    }
                    else
                    {
                        Console.WriteLine("Não existem viaturas disponiveis");
                        Console.ReadKey();
                        return (viaturasDisponiveis);
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            }

            //pede ao serviço REST a validação de um profissional de saude através de codigo profssional e numero de cedula e recebe esse objeto
            ProfissionalSaude GetProfissionalSaude()
            {
                string requestUri;
                ProfissionalSaude profissionalSaude;
                HttpResponseMessage response;

                Console.WriteLine("Autenticacao: ");
                Console.WriteLine("Codigo do Profissional: ");
                int codProfissional = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Numero de Cedula do Profissional: ");
                int nrCedula = Convert.ToInt32(Console.ReadLine());
                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/Profissional/{codProfissional}/{nrCedula}";

                try
                {
                    response = clientRest.GetAsync(requestUri).Result;
                    if (!response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        throw new Exception(response.Content.ReadAsStringAsync().Result);
                    }

                    profissionalSaude = response.Content.ReadAsAsync<ProfissionalSaude>().Result;
                    if (profissionalSaude != null)
                    {
                        Console.WriteLine($" Bem vindo: {profissionalSaude.Nome}");
                        Console.ReadKey();
                        return (profissionalSaude);

                    }
                    else
                    {
                        Console.WriteLine("Este Profissinal de Saude nao esta registado na rede");
                        Console.ReadKey();
                        return (profissionalSaude);
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            }

            // envia um de requisição de viatura
            void RegistoPedidoRequisicao()
            {
                string requestUri;
                // recebe um profissional de saude valido e autenticado através da função  GetProfissionalSaude()
                ProfissionalSaude profissionalSaude = GetProfissionalSaude();
                //invoca este metodo para no serviço REST pedir uma atualização do estado das viaturas na base de dados(em que a data de fim de uso já ultrapassou a data atual do sistema)
                AtualizaEstadoViaturas();
                //recbe uma lista de viaturas disponiveis 
                List<ViaturasDisponiveis> viaturasDisponiveis = GetViaturasDisponiveis();

                Console.Clear();
                Console.WriteLine("As viaturas disponiveis sao: ");
                foreach (ViaturasDisponiveis viaturaDisponivel in viaturasDisponiveis)
                {
                    Console.WriteLine($"Codigo:{viaturaDisponivel.Cod_viatura} Matricula: {viaturaDisponivel.Matricula} Marca:{viaturaDisponivel.Marca}");
                }
                Console.WriteLine("Escolha o codigo da viatura a reservar: ");
                int codViatura = Convert.ToInt32(Console.ReadLine());

                ViaturasDisponiveis viatura = viaturasDisponiveis.FirstOrDefault(v => v.Cod_viatura.Equals(codViatura));

                HttpResponseMessage response;
                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/api/RegistoViaturas";

                try
                {
                    DateTime dateNow = DateTime.Now;
                    // post um novo registo em que todos os parametros foram valiados anteriormente
                    response = clientRest.PostAsJsonAsync<RegistoRequisicao>(requestUri, new RegistoRequisicao
                    {
                        Cod_profissional = profissionalSaude.Cod_profissional,
                        Cod_viatura = viatura.Cod_viatura,
                        Data_pedido = dateNow,
                        Aceite = false,
                        Data_inicio_uso = viatura.Hora_inicio_uso,
                        Data_fim_uso = viatura.Hora_fim_uso
                    }).Result;
                    Console.WriteLine("Viatura Requisitada com sucesso. ");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

            //metodo que atualiza o estado das viaturas na base de dados automaticamente
            void AtualizaEstadoViaturas()
            {
                HttpResponseMessage response;
                string requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/api/RegistoViaturas";
                response = clientRest.PutAsJsonAsync<bool>(requestUri, true).Result;
                bool estado = response.Content.ReadAsAsync<bool>().Result;

                if (estado == true)
                {
                    Console.WriteLine("Estado das viaturas atualizados.");
                    return;
                }
                else
                {
                    Console.WriteLine("Nao foi possivel atualizar os estados das viaturas.");
                    return;
                }
            }

            #endregion

            #region Methods Rest Encomendas
            //pede ao serviço a criação de uma nova encomendas e material 
            void CriaEncomenda()
            { 
                // recebe um a unidade fisica valida através da função GetUnidadeFisica()
                UnidadeSaudeFisica unidadeSaudeFisica = GetUnidadeFisica();
                List<EncomendaProduto> encomendaProdutos = new List<EncomendaProduto>();
                string requestUri;
                HttpResponseMessage response;
                requestUri = $"https://{ host }:{ portEncomendasEstado.ToString().Trim() }/Encomenda";

                try
                {
                    DateTime dateNow = DateTime.Now;
                    // cria uma nova encomenda 
                    response = clientRest.PostAsJsonAsync<EncomendaEstado>(requestUri, new EncomendaEstado
                    {
                        Cod_unidade_fisica = unidadeSaudeFisica.Cod_unidade_fisica,
                        Data_pedido = dateNow,
                        Estado = false
                    }).Result;
                    var encomendaCod = response.Content.ReadAsAsync<int>().Result;
                    int codArtigo = 0;
                    int quantidade = 0;
                    do
                    {
                        Console.WriteLine("Indique o codigo de catálogo do item a encomendar: ");
                        Console.WriteLine("0 - para sair.");
                        codArtigo = Convert.ToInt32(Console.ReadLine());
                        if (codArtigo != 0)
                        {
                            Console.WriteLine($"Indique a Quantidade do artigo {codArtigo} a Encomendar ");
                            quantidade = Convert.ToInt32(Console.ReadLine());
                            if (quantidade > 0)
                            {
                                encomendaProdutos.Add(new EncomendaProduto
                                {
                                    Cod_encomenda = encomendaCod,
                                    Cod_produto = codArtigo,
                                    Quantidade = quantidade
                                });
                            }
                            else
                            {
                                Console.WriteLine("Inseriu quantidade 0 ou menor, operacao abortada...");
                                Console.ReadKey();
                                continue;
                            }
                        }

                    } while (codArtigo != 0);
                    requestUri = $"https://{ host }:{ portEncomendasEstado.ToString().Trim() }/Produtos";
                    // adiciona o material à encomenda criada 
                    response = clientRest.PostAsJsonAsync<List<EncomendaProduto>>(requestUri, encomendaProdutos).Result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
            #endregion

            #region DateUserInput e Verificação de Utente Internado ou Não
            // recebe user input para verificar se inseriu uma data valida
            DateTime GetDateFromUser(string promptText)
            {
                DateTime date = DateTime.MinValue;
                do
                {
                    Console.WriteLine(promptText);
                    string strDateString = Console.ReadLine();
                    if (!DateTime.TryParse(strDateString, out date))
                    {
                        Console.WriteLine("That is an invalid date format.  Please try something like '3/14/2015' or '5-22-2015'");
                    }
                } while (date == DateTime.MinValue);
                return date;
            }

            // pede a serviço REST a verificação na base de dados se um doente se encontra internado 
            bool VerificaUtenteIternadoNaRede(Utente utente, UnidadeSaudeFisica unidadeSaudeFisica)
            {
                string requestUri;
                HttpResponseMessage response;
                Internado internado = new Internado { CodUtente = utente.Cod_utente, CodUnidade = unidadeSaudeFisica.Cod_unidade_fisica };
                bool estaInternado;

                requestUri = $"https://{ host }:{ portRest.ToString().Trim() }/UtenteInternado";

                try
                {
                    response = clientRest.PostAsJsonAsync(requestUri, internado).Result;
                    if (!response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        throw new Exception(response.Content.ReadAsStringAsync().Result);
                    }

                    estaInternado = response.Content.ReadAsAsync<bool>().Result;
                    if (estaInternado == true)
                    {
                        return (true);
                    }

                    else
                    {
                        return (false);
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return (false);
                }
            }
            #endregion

        }
    }
}
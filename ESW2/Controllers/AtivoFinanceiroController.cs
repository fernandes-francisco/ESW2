﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESW2.Context;
using ESW2.Entities;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace ESW2.Controllers
{
    [Authorize]
    public class AtivoFinanceiroController : Controller
    {
        private readonly MyDbContext _context;

        public AtivoFinanceiroController(MyDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetCurrentClienteId()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            var clienteInfo = await _context.utilizador_clientes
                .AsNoTracking()
                .Include(uc => uc.id_utilizadorNavigation)
                .FirstOrDefaultAsync(uc => uc.id_utilizadorNavigation.username == username);

            return clienteInfo?.id_cliente;
        }

        private async Task ReloadCreateViewData(string tipoAtivo)
        {
            ViewBag.TipoAtivo = tipoAtivo ?? "Deposito";

            ViewBag.Bancos = await _context.bancos
                .OrderBy(b => b.nome_banco)
                .ToListAsync();

            if (tipoAtivo == "Deposito" || tipoAtivo == null)
            {
                ViewBag.Depositos = await _context.deposito_prazos
                    .Include(d => d.id_bancoNavigation)
                    .OrderBy(d => d.id_bancoNavigation.nome_banco)
                    .ThenBy(d => d.numero_conta_banco)
                    .ToListAsync();
            }
            else if (tipoAtivo == "Fundo")
            {
                ViewBag.Fundos = await _context.fundo_investimentos
                    .OrderBy(f => f.nome)
                    .ToListAsync();
            }
            else if (tipoAtivo == "Imovel")
            {
                ViewBag.Imoveis = await _context.imovel_arrendados
                    .OrderBy(i => i.designacao)
                    .ToListAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized("Perfil de cliente não encontrado ou utilizador não autenticado.");
            }

            var ativos = await _context.ativo_financeiros
                .Where(a => a.id_cliente == clienteId.Value)
                .Include(a => a.id_depositoNavigation)
                    .ThenInclude(d => d.id_bancoNavigation)
                .Include(a => a.id_fundoNavigation)
                .Include(a => a.id_imovelNavigation)
                .OrderByDescending(a => a.data_inicio)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            ViewBag.InfoMessage = TempData["InfoMessage"];

            return View(ativos);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound("ID do ativo não fornecido.");
            }

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized("Perfil de cliente não encontrado.");
            }

            var ativo_financeiro = await _context.ativo_financeiros
                .Include(a => a.id_clienteNavigation)
                    .ThenInclude(uc => uc.id_utilizadorNavigation)
                .Include(a => a.id_depositoNavigation)
                    .ThenInclude(d => d.id_bancoNavigation)
                .Include(a => a.id_fundoNavigation)
                .Include(a => a.id_imovelNavigation)
                .Include(a => a.id_adminNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.id_ativo == id);

            if (ativo_financeiro == null)
            {
                return NotFound($"Ativo financeiro com ID {id} não encontrado.");
            }

            if (ativo_financeiro.id_cliente != clienteId.Value)
            {
                Console.WriteLine($"Unauthorized access attempt: User (Client ID {clienteId.Value}) tried to access Ativo ID {id} owned by Client ID {ativo_financeiro.id_cliente}.");
                return Unauthorized("Não tem permissão para visualizar os detalhes deste ativo.");
            }

            return View(ativo_financeiro);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string tipoAtivo = null)
        {
            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized("Perfil de cliente não encontrado para criar ativo.");
            }

            string effectiveTipoAtivo = tipoAtivo ?? "Deposito";
            await ReloadCreateViewData(effectiveTipoAtivo);

            var newModel = new ativo_financeiro
            {
                data_inicio = DateOnly.FromDateTime(DateTime.Today),
                estado = estado_ativo.Ativo
            };

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(newModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("data_inicio,duracao_meses,percentual_imposto,id_fundo,id_imovel,id_deposito,estado")] ativo_financeiro ativo,
            string tipoAtivo,
            int? id_banco_novo = null, string numero_conta_banco_novo = null, string titulares_novo = null, double? valor_deposito_novo = null, double? taxa_juro_anual_novo = null,
            string nome_fundo_novo = null, double? valor_investido_novo = null, double? taxa_juro_padrao_novo = null,
            string designacao_nova = null, string localizacao_nova = null, double? valor_imovel_novo = null, double? valor_renda_nova = null,
            double? valor_mensal_cond_novo = null, double? valor_anual_despesas_novo = null)
        {
            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                ModelState.AddModelError("", "Não foi possível identificar o cliente. Faça login novamente.");
                await ReloadCreateViewData(tipoAtivo);
                return View(ativo);
            }

            ativo.id_cliente = clienteId.Value;
            ativo.id_admin = null;

            if (ativo.data_inicio == default) ativo.data_inicio = DateOnly.FromDateTime(DateTime.Today);
            if (ativo.estado == default) ativo.estado = estado_ativo.Ativo;
            if (ativo.duracao_meses <= 0) ModelState.AddModelError("duracao_meses", "A duração em meses deve ser um número positivo.");
            if (ativo.percentual_imposto < 0) ModelState.AddModelError("percentual_imposto", "O percentual de imposto não pode ser negativo.");

            try
            {
                if (tipoAtivo == "Deposito")
                {
                    ativo.id_fundo = null;
                    ativo.id_imovel = null;

                    if (!ativo.id_deposito.HasValue && id_banco_novo.HasValue && !string.IsNullOrEmpty(numero_conta_banco_novo))
                    {
                        var banco = await _context.bancos.FindAsync(id_banco_novo.Value);
                        if (banco == null) { ModelState.AddModelError("id_banco_novo", "O banco selecionado para o novo depósito não existe."); }
                        else
                        {
                            var novoDeposito = new deposito_prazo
                            {
                                id_banco = id_banco_novo.Value,
                                numero_conta_banco = numero_conta_banco_novo,
                                titulares = titulares_novo ?? "N/A",
                                valor_deposito = valor_deposito_novo ?? 0,
                                taxa_juro_anual = taxa_juro_anual_novo ?? 0
                            };
                            _context.deposito_prazos.Add(novoDeposito);
                            await _context.SaveChangesAsync();
                            ativo.id_deposito = novoDeposito.id_deposito;
                            Console.WriteLine($"Novo Deposito criado com ID: {novoDeposito.id_deposito}. Ativo FK set to: {ativo.id_deposito}");
                        }
                    }
                    else if (ativo.id_deposito.HasValue && !await _context.deposito_prazos.AnyAsync(d => d.id_deposito == ativo.id_deposito.Value))
                    { ModelState.AddModelError("id_deposito", "O depósito selecionado não foi encontrado."); }
                    else if (!ativo.id_deposito.HasValue)
                    { ModelState.AddModelError("id_deposito", "Deve preencher os campos para criar um novo depósito."); }
                }
                else if (tipoAtivo == "Fundo")
                {
                    ativo.id_deposito = null;
                    ativo.id_imovel = null;

                    // Since we're only creating new funds, require nome_fundo_novo and validate
                    if (string.IsNullOrEmpty(nome_fundo_novo))
                    {
                        ModelState.AddModelError("nome_fundo_novo", "O nome do fundo é obrigatório.");
                    }
                    else
                    {
                        var novoFundo = new fundo_investimento
                        {
                            nome = nome_fundo_novo,
                            valor_investido = valor_investido_novo ?? 0,
                            taxa_juro_padrao = taxa_juro_padrao_novo ?? 0
                        };
                        _context.fundo_investimentos.Add(novoFundo);
                        await _context.SaveChangesAsync();
                        ativo.id_fundo = novoFundo.id_fundo;
                        Console.WriteLine($"Novo Fundo criado com ID: {novoFundo.id_fundo}. Ativo FK set to: {ativo.id_fundo}");
                    }
                }
                else if (tipoAtivo == "Imovel")
                {
                    ativo.id_deposito = null;
                    ativo.id_fundo = null;

                    if (!ativo.id_imovel.HasValue && !string.IsNullOrEmpty(designacao_nova) && !string.IsNullOrEmpty(localizacao_nova))
                    {
                        var novoImovel = new imovel_arrendado
                        {
                            designacao = designacao_nova,
                            localizacao = localizacao_nova,
                            valor_imovel = valor_imovel_novo ?? 0,
                            valor_renda = valor_renda_nova ?? 0,
                            valor_mensal_cond = valor_mensal_cond_novo ?? 0,
                            valor_anual_despesas = valor_anual_despesas_novo ?? 0
                        };
                        _context.imovel_arrendados.Add(novoImovel);
                        await _context.SaveChangesAsync();
                        ativo.id_imovel = novoImovel.id_imovel;
                        Console.WriteLine($"Novo Imovel criado com ID: {novoImovel.id_imovel}. Ativo FK set to: {ativo.id_imovel}");
                    }
                    else if (ativo.id_imovel.HasValue && !await _context.imovel_arrendados.AnyAsync(i => i.id_imovel == ativo.id_imovel.Value))
                    { ModelState.AddModelError("id_imovel", "O imóvel selecionado não foi encontrado."); }
                    else if (!ativo.id_imovel.HasValue)
                    { ModelState.AddModelError("id_imovel", "Deve preencher os campos para criar um novo imóvel."); }
                }
                else
                {
                    ModelState.AddModelError("", "Tipo de ativo inválido ou não selecionado.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Erro ao salvar entidade relacionada ({tipoAtivo}): {dbEx.ToString()}");
                ModelState.AddModelError("", $"Erro ao criar o novo {tipoAtivo}: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado ao processar entidade relacionada ({tipoAtivo}): {ex.ToString()}");
                ModelState.AddModelError("", $"Erro inesperado ao processar o {tipoAtivo}: {ex.Message}");
            }

            int typeCount = (ativo.id_fundo.HasValue ? 1 : 0) +
                            (ativo.id_imovel.HasValue ? 1 : 0) +
                            (ativo.id_deposito.HasValue ? 1 : 0);

            if (typeCount == 0 && ModelState.ErrorCount == 0)
            { ModelState.AddModelError("", "Falha ao associar o ativo a um Fundo, Imóvel ou Depósito."); }
            else if (typeCount > 1)
            { ModelState.AddModelError("", "Erro interno: Tentativa de associar o ativo a múltiplos tipos de investimento."); }

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine($"Tentando salvar Ativo Financeiro: Cliente={ativo.id_cliente}, Data={ativo.data_inicio}, Duracao={ativo.duracao_meses}, Imposto={ativo.percentual_imposto}%, Tipo={tipoAtivo}, DepositoId={ativo.id_deposito}, FundoId={ativo.id_fundo}, ImovelId={ativo.id_imovel}");

                    _context.ativo_financeiros.Add(ativo);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Ativo Financeiro salvo com ID: {ativo.id_ativo}");
                    TempData["SuccessMessage"] = $"Ativo financeiro ({tipoAtivo}) criado com sucesso!";
                    return RedirectToAction("Create", new { tipoAtivo = tipoAtivo });
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"DbUpdateException ao salvar AtivoFinanceiro: {ex.ToString()}");
                    ModelState.AddModelError("", $"Não foi possível guardar o ativo financeiro. Verifique os dados. Detalhe: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception ao salvar AtivoFinanceiro: {ex.ToString()}");
                    ModelState.AddModelError("", $"Ocorreu um erro inesperado ao guardar o ativo: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("ModelState Inválido na criação:");
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        Console.WriteLine($"  Campo '{state.Key}': {string.Join("; ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            await ReloadCreateViewData(tipoAtivo);
            ViewBag.id_banco_novo = id_banco_novo;
            ViewBag.numero_conta_banco_novo = numero_conta_banco_novo;
            ViewBag.titulares_novo = titulares_novo;
            ViewBag.valor_deposito_novo = valor_deposito_novo;
            ViewBag.taxa_juro_anual_novo = taxa_juro_anual_novo;
            ViewBag.nome_fundo_novo = nome_fundo_novo;
            ViewBag.valor_investido_novo = valor_investido_novo;
            ViewBag.taxa_juro_padrao_novo = taxa_juro_padrao_novo;
            ViewBag.designacao_nova = designacao_nova;
            ViewBag.localizacao_nova = localizacao_nova;
            ViewBag.valor_imovel_novo = valor_imovel_novo;
            ViewBag.valor_renda_nova = valor_renda_nova;
            ViewBag.valor_mensal_cond_novo = valor_mensal_cond_novo;
            ViewBag.valor_anual_despesas_novo = valor_anual_despesas_novo;

            return View(ativo);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound("ID do ativo não fornecido.");

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null) return Unauthorized("Perfil de cliente não encontrado.");

            var ativo_financeiro = await _context.ativo_financeiros
                .Include(a => a.id_depositoNavigation).ThenInclude(d => d.id_bancoNavigation)
                .Include(a => a.id_fundoNavigation)
                .Include(a => a.id_imovelNavigation)
                .FirstOrDefaultAsync(a => a.id_ativo == id);

            if (ativo_financeiro == null) return NotFound($"Ativo financeiro com ID {id} não encontrado.");
            if (ativo_financeiro.id_cliente != clienteId.Value) return Unauthorized("Não tem permissão para editar este ativo.");

            return View(ativo_financeiro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("id_ativo,data_inicio,duracao_meses,percentual_imposto,estado")] ativo_financeiro ativoFromForm)
        {
            if (id != ativoFromForm.id_ativo) return BadRequest("Inconsistência no ID do ativo.");

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null) return Unauthorized("Perfil de cliente não encontrado.");

            var originalAsset = await _context.ativo_financeiros
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.id_ativo == id);

            if (originalAsset == null) return NotFound($"Ativo financeiro com ID {id} não encontrado para edição.");
            if (originalAsset.id_cliente != clienteId.Value) return Unauthorized("Não tem permissão para editar este ativo.");

            ativoFromForm.id_cliente = originalAsset.id_cliente;
            ativoFromForm.id_admin = originalAsset.id_admin;
            ativoFromForm.id_deposito = originalAsset.id_deposito;
            ativoFromForm.id_fundo = originalAsset.id_fundo;
            ativoFromForm.id_imovel = originalAsset.id_imovel;

            if (ativoFromForm.duracao_meses <= 0) ModelState.AddModelError("duracao_meses", "Duração deve ser positiva.");
            if (ativoFromForm.percentual_imposto < 0) ModelState.AddModelError("percentual_imposto", "Imposto não pode ser negativo.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ativoFromForm);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Ativo financeiro atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ativo_financeiroExists(ativoFromForm.id_ativo)) return NotFound("Ativo foi eliminado enquanto editava.");
                    else ModelState.AddModelError("", "Este ativo foi modificado. Recarregue a página e tente novamente.");
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"DbUpdateException ao editar AtivoFinanceiro ID {id}: {ex.ToString()}");
                    ModelState.AddModelError("", $"Não foi possível guardar as alterações. Detalhe: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception ao editar AtivoFinanceiro ID {id}: {ex.ToString()}");
                    ModelState.AddModelError("", $"Erro inesperado ao guardar alterações: {ex.Message}");
                }
            }

            Console.WriteLine("ModelState Inválido na edição.");
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    Console.WriteLine($"  {state.Key}: {string.Join(";", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            return View(ativoFromForm);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound("ID do ativo não fornecido.");

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null) return Unauthorized("Perfil de cliente não encontrado.");

            var ativo_financeiro = await _context.ativo_financeiros
                .Include(a => a.id_depositoNavigation).ThenInclude(d => d.id_bancoNavigation)
                .Include(a => a.id_fundoNavigation)
                .Include(a => a.id_imovelNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.id_ativo == id);

            if (ativo_financeiro == null)
            {
                TempData["InfoMessage"] = $"Ativo financeiro com ID {id} não encontrado ou já foi eliminado.";
                return RedirectToAction(nameof(Index));
            }

            if (ativo_financeiro.id_cliente != clienteId.Value)
            {
                TempData["ErrorMessage"] = "Não tem permissão para eliminar este ativo.";
                return RedirectToAction(nameof(Index));
            }

            return View(ativo_financeiro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                TempData["ErrorMessage"] = "Perfil de cliente não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var ativo_financeiro = await _context.ativo_financeiros
                .Include(a => a.id_depositoNavigation)
                .Include(a => a.id_fundoNavigation)
                .Include(a => a.id_imovelNavigation)
                .FirstOrDefaultAsync(a => a.id_ativo == id && a.id_cliente == clienteId.Value);

            if (ativo_financeiro == null)
            {
                TempData["InfoMessage"] = "Ativo financeiro não encontrado ou você não tem permissão para eliminá-lo.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var hasRelatedRecords = await _context.pagamento_impostos
                    .AnyAsync(pi => pi.id_ativo == id);

                if (hasRelatedRecords)
                {
                    TempData["ErrorMessage"] = "Não é possível eliminar este ativo porque ele possui impostos pagos associados.";
                    return RedirectToAction(nameof(Index));
                }

                if (ativo_financeiro.id_deposito.HasValue)
                {
                    var deposito = await _context.deposito_prazos
                        .FirstOrDefaultAsync(d => d.id_deposito == ativo_financeiro.id_deposito.Value);
                    if (deposito != null)
                    {
                        _context.deposito_prazos.Remove(deposito);
                        Console.WriteLine($"Depósito ID {deposito.id_deposito} marcado para exclusão.");
                    }
                }
                else if (ativo_financeiro.id_fundo.HasValue)
                {
                    var fundo = await _context.fundo_investimentos
                        .FirstOrDefaultAsync(f => f.id_fundo == ativo_financeiro.id_fundo.Value);
                    if (fundo != null)
                    {
                        _context.fundo_investimentos.Remove(fundo);
                        Console.WriteLine($"Fundo ID {fundo.id_fundo} marcado para exclusão.");
                    }
                }
                else if (ativo_financeiro.id_imovel.HasValue)
                {
                    var imovel = await _context.imovel_arrendados
                        .FirstOrDefaultAsync(i => i.id_imovel == ativo_financeiro.id_imovel.Value);
                    if (imovel != null)
                    {
                        _context.imovel_arrendados.Remove(imovel);
                        Console.WriteLine($"Imóvel ID {imovel.id_imovel} marcado para exclusão.");
                    }
                }

                _context.ativo_financeiros.Remove(ativo_financeiro);
                Console.WriteLine($"Ativo Financeiro ID {ativo_financeiro.id_ativo} marcado para exclusão.");

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ativo financeiro e registro associado eliminados com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"DbUpdateException ao eliminar AtivoFinanceiro ID {id} ou registro associado: {ex.ToString()}");
                TempData["ErrorMessage"] = $"Erro ao eliminar o ativo ou registro associado: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception ao eliminar AtivoFinanceiro ID {id} ou registro associado: {ex.ToString()}");
                TempData["ErrorMessage"] = $"Erro inesperado ao eliminar o ativo ou registro associado: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool ativo_financeiroExists(int id)
        {
            return _context.ativo_financeiros.Any(e => e.id_ativo == id);
        }
    }
}
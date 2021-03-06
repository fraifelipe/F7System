﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using F7System.Api.Domain.Commands;
using F7System.Api.Domain.Commands.Estudante;
using F7System.Api.Domain.Commands.Matricula;
using F7System.Api.Domain.Models;
using F7System.Api.Domain.Services;
using F7System.Api.Infrastructure.Models;
using F7System.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace F7System.Api.Domain.CommandHandlers
{
    public class PessoaCommandHandler :
        IRequestHandler<CriarPessoaCommand>,
        IRequestHandler<AlterarPessoaCommand>,
        IRequestHandler<DeletarPessoaCommand>,
        IRequestHandler<AddMatriculaEstudanteCommand>,
        IRequestHandler<AddInscricoesMatriculaEstudanteCommand>

    {
        private readonly IUserService _userService;
        private readonly F7DbContext _f7DbContext;
        private readonly IMediator _mediator;

        public PessoaCommandHandler(IUserService userService, F7DbContext f7DbContext, IMediator mediator)
        {
            _userService = userService;
            _f7DbContext = f7DbContext;
            _mediator = mediator;
        }

        public Task<Unit> Handle(CriarPessoaCommand request, CancellationToken cancellationToken)
        {
            var student = new PessoaUsuario()
            {
                Id = request.Id,
                Nome = request.Nome,
                CPF = request.CPF,
                DataNascimento = request.DataNascimento,
                Perfil = request.Perfil
            };

            _f7DbContext.Add(student);
            _f7DbContext.SaveChanges();

            var login = new LoginModel()
            {
                Username = request.Username,
                Password = request.Password
            };

            _userService.GiveAccess(student, login);

            return Unit.Task;
        }

        public Task<Unit> Handle(AlterarPessoaCommand request, CancellationToken cancellationToken)
        {
            var student = _f7DbContext.PessoaUsuarioDbSet.FirstOrDefault(x => x.Id == request.Id);

            student.Nome = request.Nome;
            student.Username = request.Username;
            student.CPF = request.CPF;
            student.DataNascimento = request.DataNascimento;
            student.Perfil = student.Perfil;
            _f7DbContext.SaveChanges();

            return Unit.Task;
        }

        public Task<Unit> Handle(DeletarPessoaCommand request, CancellationToken cancellationToken)
        {
            var estudante = _f7DbContext.PessoaUsuarioDbSet.FirstOrDefault(x => x.Id == request.Id);
            _f7DbContext.Remove(estudante);
            _f7DbContext.SaveChanges();

            return Unit.Task;
        }

        public Task<Unit> Handle(AddMatriculaEstudanteCommand request, CancellationToken cancellationToken)
        {
            var estudante = _f7DbContext.PessoaUsuarioDbSet.FirstOrDefault(x => x.Id == request.PessoaId);
            var grade = _f7DbContext.GradeDbSet.FirstOrDefault(x => x.Id == request.GradeId);

            if (estudante != null && grade != null)
            {
                var matricula = new Matricula()
                {
                    Id = request.MatriculaId,
                    PessoaUsuario = estudante,
                    PessoaUsuarioId = estudante.Id,
                    Grade = grade
                };
                _f7DbContext.Add(matricula);
                estudante.Matriculas.Add(matricula);
            }

            var cmd = new AtivarMatriculaCommand()
            {
                MatriculaId = request.MatriculaId
            };

            _mediator.Send(cmd, cancellationToken);

            _f7DbContext.SaveChanges();

            return Unit.Task;
        }

        public Task<Unit> Handle(AddInscricoesMatriculaEstudanteCommand request, CancellationToken cancellationToken)
        {
            var config = _f7DbContext.Configuration;
            
            var matricula = _f7DbContext.MatriculaDbSet.Include(x => x.Inscricoes).ThenInclude(x => x.Turma)
                .FirstOrDefault(x => x.Id == request.MatriculaId);


            if (matricula != null)
            {
                var turmasAtuais = matricula.Inscricoes.Where(x => x.Turma.Semestre == config.SemestreAtual).Select(x => x.Turma.Id).ToList();
                var novasTurmasIds = request.TurmaIds.Except(turmasAtuais);

                var turmasExcluidas = turmasAtuais.Except(request.TurmaIds);

                matricula.Inscricoes.RemoveAll(x => x.Turma.Semestre == config.SemestreAtual && turmasExcluidas.Contains(x.Turma.Id));
                
                var turmas = _f7DbContext.TurmaDbSet.Where(x => novasTurmasIds.Contains(x.Id));
                var inscricoes = turmas.Select(turma => new Inscricao()
                {
                    Id = Guid.NewGuid(),
                    Matricula = matricula,
                    MatriculaId = matricula.Id,
                    Turma = turma,
                    DataInscricao = DateTime.Now
                }).ToList();

                matricula.Inscricoes.AddRange(inscricoes);

                _f7DbContext.AddRange(inscricoes);
                _f7DbContext.SaveChanges();
            }

            return Unit.Task;
        }
    }
}
﻿using System;
using System.Collections.Generic;

namespace F7System.Api.Domain.Models
{
    public class Inscricao
    {
        public Guid Id { get; set; }
        public Turma Turma { get; set; }
        public DateTime DataInscricao { get; set; }
        public decimal Nota { get; set; }
    }
}
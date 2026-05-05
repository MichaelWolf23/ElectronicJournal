using System;
using System.Collections.Generic;
using ElectronicJournal.Services;

namespace ElectronicJournal.Repositories;

public abstract class RepositoryBase
{
    protected RepositoryBase(DatabaseService databaseService)
    {
        DatabaseService = databaseService;
    }

    protected DatabaseService DatabaseService { get; }
}


using Unit = Microsoft.VisualStudio.TestTools.UnitTesting;

//https://github.com/Microsoft/perfview/issues/769

//You can have at most 8 active sessions on a given provider. A list of all active providers can be viewed with
//logman query -ets|findstr DebugTools
//To protect ourselves, we limit our tests to just 6 concurrent providers
[assembly: Unit.Parallelize(Scope = Unit.ExecutionScope.MethodLevel, Workers = 6)]

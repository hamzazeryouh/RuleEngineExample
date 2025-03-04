using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RulesEngine.Models;
using RulesEngine;

public class CumacService
{
    private readonly RulesEngine.RulesEngine _rulesEngine;

    public CumacService()
    {
        try
        {
            string filePath = "C:\\Users\\perfectshore\\source\\repos\\RuleEngineExample\\RuleEngineExample\\rules.json";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Le fichier rules.json est introuvable.", filePath);
            }

            var json = File.ReadAllText(filePath);
            var workflows = JsonConvert.DeserializeObject<Workflow[]>(json);

            if (workflows == null || workflows.Length == 0)
            {
                throw new InvalidOperationException("Les règles CUMAC ne sont pas valides ou sont vides.");
            }

            _rulesEngine = new RulesEngine.RulesEngine(workflows);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur lors du chargement des règles : {ex.Message}");
            throw;
        }
    }

    public async Task<CumacResult> CalculerCumacAsync(OperationTravaux operation)
{
    if (operation == null)
    {
        throw new ArgumentNullException(nameof(operation), "L'opération ne peut pas être nulle.");
    }

    var inputs = new List<RuleParameter> { new RuleParameter("input1", operation) };

    var cumacResults = await _rulesEngine.ExecuteAllRulesAsync("CUMAC-Calculation", inputs.ToArray());

    var result = new CumacResult
    {
        ReferenceValue = operation.ReferenceValue,
        CumacStandard = 0M,
        CumacCoupDePouce = 0M,
        CumacZNI = 0M
    };

    Console.WriteLine($"\n?? Processing: {JsonConvert.SerializeObject(operation, Formatting.Indented)}");

    foreach (var ruleResult in cumacResults)
    {
        if (ruleResult.IsSuccess && ruleResult.ActionResult?.Output != null)
        {
            if (decimal.TryParse(ruleResult.ActionResult.Output.ToString(), out decimal value))
            {
                if (ruleResult.Rule.RuleName == "BaseCumacCalculation")
                {
                    result.CumacStandard = value;
                }
                else if (ruleResult.Rule.RuleName == "ApplyCoupDePouce")
                {
                    result.CumacCoupDePouce = value;
                }
                else if (ruleResult.Rule.RuleName == "ZNIPrecary" || ruleResult.Rule.RuleName == "ZNIStandard")
                {
                    result.CumacZNI = value;
                }

                Console.WriteLine($"✅ Rule {ruleResult.Rule.RuleName} PASSED. Value: {value} kWhc");
            }
            else
            {
                Console.WriteLine($"⚠️ Impossible de convertir le résultat de {ruleResult.Rule.RuleName} en decimal.");
            }
        }
        else
        {
            Console.WriteLine($"❌ Rule {ruleResult.Rule.RuleName} FAILED. Reason: {ruleResult.Rule.ErrorMessage}");
        }
    }

    return result;
}

}

public class Program
{
    public static async Task Main(string[] args)
    {
        var cumacService = new CumacService();

        var operations = new List<OperationTravaux>
        {
            new OperationTravaux { ReferenceValue = 40000M, CoefficientZone = 1.1M, CoefficientOperation = 1.3M, IsCoupDePouce = false, IsZNI = false },
            new OperationTravaux { ReferenceValue = 50000M, CoefficientZone = 1.2M, CoefficientOperation = 1.5M, IsCoupDePouce = true, IsZNI = false },
            new OperationTravaux { ReferenceValue = 60000M, CoefficientZone = 1.3M, CoefficientOperation = 1.6M, IsCoupDePouce = false, IsZNI = true },
            new OperationTravaux { ReferenceValue = 70000M, CoefficientZone = 1.5M, CoefficientOperation = 1.8M, IsCoupDePouce = true, IsZNI = true }
        };

        var results = new List<CumacResult>();

        foreach (var operation in operations)
        {
            var cumacResult = await cumacService.CalculerCumacAsync(operation);
            results.Add(cumacResult);
        }

        Console.WriteLine("\n📊 Résultats du CUMAC pour chaque opération :\n");

        foreach (var result in results)
        {
            Console.WriteLine($"🔹 Référence : {result.ReferenceValue} kWh");
            Console.WriteLine($"✅ Standard : {result.CumacStandard} kWhc");
            Console.WriteLine($"🔥 Coup de Pouce : {result.CumacCoupDePouce} kWhc");
            Console.WriteLine($"🌍 ZNI : {result.CumacZNI} kWhc");
            Console.WriteLine("------------------------------------------------");
        }
    }
}

public class OperationTravaux
{
    public decimal ReferenceValue { get; set; }
    public decimal CoefficientZone { get; set; }
    public decimal CoefficientOperation { get; set; }
    public bool IsCoupDePouce { get; set; }
    public bool IsZNI { get; set; }
}

public class CumacResult
{
    public decimal ReferenceValue { get; set; }
    public decimal CumacStandard { get; set; }
    public decimal CumacCoupDePouce { get; set; }
    public decimal CumacZNI { get; set; }
}

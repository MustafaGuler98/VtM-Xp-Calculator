using System;
using System.Globalization;

public sealed class VtmXpCalculator
{
    // --- Constants (balance here) ---
    private const int AgeBracket1 = 100;
    private const int AgeBracket2 = 200;
    private const int AgeBracket3 = 500;
    private const int AgeBracket4 = 1500;

    private const double TalentWeight = 3.0;
    private const double DisciplineWeight = 3.0;
    private const double KnowledgeAccessWeight = 2.5;
    private const double GenerationWeight = 1.5;

    private const double AveragePotentialScore = 50.0;
    private const double MaxModifierPercentage = 0.80; // +/-80%
    private const double Exponent = 1.5;


    // Calculates raw XP by age using a continuous piecewise function.
    public double CalculateBaseXp(int age)
    {
        age = Math.Max(age, 0);

        if (age <= AgeBracket1) return 5 * Math.Sqrt(age);
        if (age <= AgeBracket2) return 50 + (age - AgeBracket1);
        if (age <= AgeBracket3) return 150 + (age - AgeBracket2) * 0.75;
        if (age <= AgeBracket4) return 375 + (age - AgeBracket3) * 0.5;
        return 875 + (age - AgeBracket4) * 0.25;
    }

    // Calculates a potential score in [10..100] from four stats.
    public double CalculatePotentialScore(int talent, int discipline, int knowledgeAccess, int generation)
    {
        // Clamp inputs to safe ranges
        talent = Math.Clamp(talent, 1, 10);
        discipline = Math.Clamp(discipline, 1, 10);
        knowledgeAccess = Math.Clamp(knowledgeAccess, 1, 10);
        generation = Math.Clamp(generation, 4, 13);

        int generationScore = 14 - generation;

        double score =
            (talent * TalentWeight) +
            (discipline * DisciplineWeight) +
            (knowledgeAccess * KnowledgeAccessWeight) +
            (generationScore * GenerationWeight);

        return Math.Clamp(score, 10.0, 100.0);
    }

   
    // Maps potential score to a final modifier in [-0.80..+0.80] using a 1.5-power curve.
    public double CalculateFinalModifier(double potentialScore)
    {
        double delta = potentialScore - AveragePotentialScore;
        if (Math.Abs(delta) < 1e-2) return 0.0;

        double k, mod;

        if (delta > 0)
        {
            // Up to +50 → +0.80
            k = MaxModifierPercentage / Math.Pow(50.0, Exponent);
            mod = k * Math.Pow(delta, Exponent);
        }
        else
        {
            // Down to -40 → -0.80
            k = MaxModifierPercentage / Math.Pow(40.0, Exponent);
            mod = -k * Math.Pow(Math.Abs(delta), Exponent);
        }

        return Math.Clamp(mod, -MaxModifierPercentage, MaxModifierPercentage);
    }

 
    // High-level helper to compute the final starting XP in one call.
    public double ComputeStartingXp(int age, int talent, int discipline, int knowledgeAccess, int generation)
    {
        double baseXp = CalculateBaseXp(age);
        double potential = CalculatePotentialScore(talent, discipline, knowledgeAccess, generation);
        double modifier = CalculateFinalModifier(potential);
        return baseXp * (1.0 + modifier);
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        var calculator = new VtmXpCalculator();

        Console.WriteLine("--- VtM Advanced Character XP Calculator (Balanced Model v1.5) ---");

        while (true)
        {
            Console.WriteLine("\n-------------------------------------------");
            int age = PromptForInteger("Enter Character's Age: ", 0, 10000);
            int talent = PromptForInteger("Talent (1-10): ", 1, 10);
            int discipline = PromptForInteger("Work Discipline (1-10): ", 1, 10);
            int knowledge = PromptForInteger("Access to Knowledge (1-10): ", 1, 10);
            int generation = PromptForInteger("Generation (4-13): ", 4, 13);

            double baseXp = calculator.CalculateBaseXp(age);
            double potential = calculator.CalculatePotentialScore(talent, discipline, knowledge, generation);
            double modifier = calculator.CalculateFinalModifier(potential);
            double totalXp = baseXp * (1 + modifier);

            Console.WriteLine("\n--- CALCULATION RESULTS ---");
            Console.WriteLine($"Base XP (From Age Only): {baseXp.ToString("F2", CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Potential Score (10-100 scale): {potential.ToString("F2", CultureInfo.InvariantCulture)}");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Potential Modifier (Balanced Model): {modifier.ToString("P2", CultureInfo.InvariantCulture)}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-----------> FINAL STARTING XP: {totalXp.ToString("F2", CultureInfo.InvariantCulture)} <-----------");
            Console.ResetColor();

            Console.Write("\nPress Enter to perform a new calculation (or type 'q' to quit): ");
            if ((Console.ReadLine() ?? string.Empty).Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
                break;
        }
    }

    // Read an integer within [min..max], culture-invariant parsing.
    private static int PromptForInteger(string message, int min, int max)
    {
        while (true)
        {
            Console.Write(message);
            string? input = Console.ReadLine();

            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) &&
                value >= min && value <= max)
            {
                return value;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid input! Please enter an integer between {min} and {max}.");
            Console.ResetColor();
        }
    }
}

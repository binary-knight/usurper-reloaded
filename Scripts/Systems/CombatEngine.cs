using UsurperReborn.Scripts.Core;

namespace UsurperReborn.Scripts.Systems
{
    public class CombatEngine
    {
        private Random random = new Random();
        
        public CombatResult ExecuteCombat(Character attacker, Character defender)
        {
            var result = new CombatResult
            {
                Attacker = attacker,
                Defender = defender,
                Rounds = new List<CombatRound>()
            };
            
            var attackerHP = attacker.CurrentHP;
            var defenderHP = defender.CurrentHP;
            
            while (attackerHP > 0 && defenderHP > 0)
            {
                var round = new CombatRound();
                
                // Attacker's turn
                var attackerDamage = CalculateDamage(attacker, defender);
                defenderHP -= attackerDamage;
                round.AttackerDamage = attackerDamage;
                
                if (defenderHP <= 0)
                {
                    result.Winner = attacker;
                    result.Loser = defender;
                    break;
                }
                
                // Defender's turn
                var defenderDamage = CalculateDamage(defender, attacker);
                attackerHP -= defenderDamage;
                round.DefenderDamage = defenderDamage;
                
                if (attackerHP <= 0)
                {
                    result.Winner = defender;
                    result.Loser = attacker;
                    break;
                }
                
                result.Rounds.Add(round);
                
                // Prevent infinite loops
                if (result.Rounds.Count > 100)
                {
                    // Determine winner by remaining HP percentage
                    var attackerPercent = (float)attackerHP / attacker.MaxHP;
                    var defenderPercent = (float)defenderHP / defender.MaxHP;
                    
                    if (attackerPercent > defenderPercent)
                    {
                        result.Winner = attacker;
                        result.Loser = defender;
                    }
                    else
                    {
                        result.Winner = defender;
                        result.Loser = attacker;
                    }
                    break;
                }
            }
            
            return result;
        }
        
        private int CalculateDamage(Character attacker, Character defender)
        {
            var baseDamage = attacker.Attack + random.Next(-2, 3); // Add some randomness
            var defense = defender.Defense;
            var damage = Math.Max(1, baseDamage - defense); // Minimum 1 damage
            
            return damage;
        }
    }
    
    public class CombatResult
    {
        public Character Attacker { get; set; }
        public Character Defender { get; set; }
        public Character Winner { get; set; }
        public Character Loser { get; set; }
        public List<CombatRound> Rounds { get; set; } = new List<CombatRound>();
    }
    
    public class CombatRound
    {
        public int AttackerDamage { get; set; }
        public int DefenderDamage { get; set; }
    }
} 
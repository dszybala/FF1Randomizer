using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RomUtilities;
using System.Collections;
using System.IO;
using System.Numerics;

namespace FF1Lib
{
	// ReSharper disable once InconsistentNaming
	public partial class FF1Rom : NesRom
	{
		public const string Version = "1.6.0";

		public const int CopyrightOffset1 = 0x384A8;
		public const int CopyrightOffset2 = 0x384BA;

		public const int RngOffset = 0x3F100;
		public const int RngSize = 256;

		public const int LevelRequirementsOffset = 0x2D000;
		public const int LevelRequirementsSize = 3;
		public const int LevelRequirementsCount = 49;

		public const int StartingGoldOffset = 0x301C;

		public const int GoldItemOffset = 108; // 108 items before gold chests
		public const int GoldItemCount = 68;

		public FF1Rom(string filename) : base(filename)
		{}

		public FF1Rom(Stream readStream) : base(readStream)
		{}

		private FF1Rom()
		{}

		public static async Task<FF1Rom> CreateAsync(Stream readStream)
		{
			var rom = new FF1Rom();
			await rom.LoadAsync(readStream);

			return rom;
		}

		public void Randomize(Blob seed, UserFlags userFlags)
		{
			var rng = new MT19337(BitConverter.ToUInt32(seed, 0));
			var flags = ChooseFlags(userFlags, rng);

			EasterEggs();
			RollCredits();

			// This has to be done before we shuffle spell levels.
			if (flags.SpellBugs)
			{
				FixSpellBugs();
			}

			if (flags.Treasures)
			{
				ShuffleTreasures(rng, flags.EarlyCanoe, flags.EarlyOrdeals, flags.IncentivizeIceCave, flags.IncentivizeOrdeals);
			}

			if (flags.Shops)
			{
				ShuffleShops(rng, flags.EnemyStatusAttacks);
			}

			if (flags.MagicShops)
			{
				ShuffleMagicShops(rng);
			}

			if (flags.MagicLevels)
			{
				ShuffleMagicLevels(rng, flags.MagicPermissions);
			}

			if (flags.Rng)
			{
				ShuffleRng(rng);
			}

			if (flags.EnemyScripts)
			{
				ShuffleEnemyScripts(rng);
			}

			if (flags.EnemySkillsSpells)
			{
				ShuffleEnemySkillsSpells(rng);
			}

			if (flags.EnemyStatusAttacks)
			{
				ShuffleEnemyStatusAttacks(rng);
			}

			if (flags.Ordeals)
			{
				ShuffleOrdeals(rng);
			}

			if (flags.EarlyOrdeals)
			{
				EnableEarlyOrdeals();
			}

			if (flags.EarlyRod)
			{
				EnableEarlyRod();
			}

			if (flags.EarlyCanoe)
			{
				EnableEarlyCanoe();
			}

			if (flags.EarlyBridge)
			{
				EnableEarlyBridge();
			}

			if (flags.NoPartyShuffle)
			{
				DisablePartyShuffle();
			}

			if (flags.SpeedHacks)
			{
				EnableSpeedHacks();
			}

			if (flags.IdentifyTreasures)
			{
				EnableIdentifyTreasures();
			}

			if (flags.Dash)
			{
				EnableDash();
			}

			if (flags.BuyTen)
			{
				EnableBuyTen();
			}

			if (flags.HouseMPRestoration)
			{
				FixHouse();
			}

			if (flags.WeaponStats)
			{
				FixWeaponStats();
			}

			if (flags.ChanceToRun)
			{
				FixChanceToRun();
			}

			if (flags.EnemyStatusAttackBug)
			{
				FixEnemyStatusAttackBug();
			}

			if (flags.FunEnemyNames)
			{
				FunEnemyNames(flags.TeamSteak);
			}

			var itemText = ReadText(ItemTextPointerOffset, ItemTextPointerBase, ItemTextPointerCount);
			itemText[99] = FF1Text.TextToBytes("Ribbon ", useDTE: false);

			ExpGoldBoost(flags.ExpBonus, flags.ExpMultiplier);
			ScalePrices(flags.PriceScaleFactor, flags.ExpMultiplier, itemText, rng);

			WriteText(itemText, ItemTextPointerOffset, ItemTextPointerBase, ItemTextOffset);

			if (flags.EnemyScaleFactor > 1)
			{
				ScaleEnemyStats(flags.EnemyScaleFactor, rng);
			}

			// This can be called with every seed, with zero forced it 
			// just shuffles the default party without forcing any selections
			PartyRandomize(rng, flags.ForcedPartyMembers);

			// We have to do "fun" stuff last because it alters the RNG state.
			if (flags.PaletteSwap)
			{
				PaletteSwap(rng);
			}

			if (flags.TeamSteak)
			{
				TeamSteak();
			}

			if (flags.Music != MusicShuffle.None)
			{
				ShuffleMusic(flags.Music, rng);
			}

			if (flags.ShuffleLeader)
			{
				ShuffleLeader(rng);
			}

			WriteSeedAndFlags(Version, seed.ToHex(), EncodeFlagsText(userFlags));
		}

		private static Flags ChooseFlags(UserFlags userFlags, MT19337 rng)
		{
			return new Flags
			{
				Treasures = ChooseFlag(userFlags.Treasures, rng),
				IncentivizeIceCave = ChooseFlag(userFlags.IncentivizeIceCave, rng),
				IncentivizeOrdeals = ChooseFlag(userFlags.IncentivizeOrdeals, rng),
				Shops = ChooseFlag(userFlags.Shops, rng),
				MagicShops = ChooseFlag(userFlags.MagicShops, rng),
				MagicLevels = ChooseFlag(userFlags.MagicLevels, rng),
				MagicPermissions = ChooseFlag(userFlags.MagicPermissions, rng),
				Rng = ChooseFlag(userFlags.Rng, rng),
				EnemyScripts = ChooseFlag(userFlags.EnemyScripts, rng),
				EnemySkillsSpells = ChooseFlag(userFlags.EnemySkillsSpells, rng),
				EnemyStatusAttacks = ChooseFlag(userFlags.EnemyStatusAttacks, rng),
				Ordeals = ChooseFlag(userFlags.Ordeals, rng),

				EarlyRod = ChooseFlag(userFlags.EarlyRod, rng),
				EarlyCanoe = ChooseFlag(userFlags.EarlyCanoe, rng),
				EarlyOrdeals = ChooseFlag(userFlags.EarlyOrdeals, rng),
				EarlyBridge = ChooseFlag(userFlags.EarlyBridge, rng),
				NoPartyShuffle = userFlags.NoPartyShuffle,
				SpeedHacks = userFlags.SpeedHacks,
				IdentifyTreasures = userFlags.IdentifyTreasures,
				Dash = userFlags.Dash,
				BuyTen =userFlags.BuyTen,

				HouseMPRestoration = userFlags.HouseMPRestoration,
				WeaponStats = userFlags.WeaponStats,
				ChanceToRun = userFlags.ChanceToRun,
				SpellBugs = userFlags.SpellBugs,
				EnemyStatusAttackBug = userFlags.EnemyStatusAttackBug,

				FunEnemyNames = ChooseFlag(userFlags.FunEnemyNames, rng),
				PaletteSwap = ChooseFlag(userFlags.PaletteSwap, rng),
				TeamSteak = ChooseFlag(userFlags.TeamSteak, rng),
				ShuffleLeader = ChooseFlag(userFlags.ShuffleLeader, rng),
				Music = userFlags.Music,

				ForcedPartyMembers = userFlags.ForcedPartyMembers,
				EnemyScaleFactor = userFlags.EnemyScaleFactor,
				PriceScaleFactor = userFlags.PriceScaleFactor,
				ExpMultiplier = userFlags.ExpMultiplier,
				ExpBonus = userFlags.ExpBonus
			};
		}

		private static bool ChooseFlag(UserFlag userFlag, MT19337 rng)
		{
			switch (userFlag)
			{
				case UserFlag.Disabled:
					return false;
				case UserFlag.Enabled:
					return true;
				case UserFlag.Random:
					return rng.Between(0, 1) == 1;
				default:
					throw new InvalidOperationException("Unexpected flag state: " + userFlag);
			}
		}

		public override bool Validate()
		{
			return Get(0, 16) == Blob.FromHex("06400e890e890e401e400e400e400b42");
		}

		public void WriteSeedAndFlags(string version, string seed, string flags)
		{
			var seedBytes = FF1Text.TextToBytes($"{version}  {seed}", useDTE: false);
			var flagBytes = FF1Text.TextToBytes($"{flags}", useDTE: false);
			var padding = new byte[15 - flagBytes.Length];
			for (int i = 0; i < padding.Length; i++)
			{
				padding[i] = 0xFF;
			}

			Put(CopyrightOffset1, seedBytes);
			Put(CopyrightOffset2, padding + flagBytes);
		}

		public void ShuffleRng(MT19337 rng)
		{
			var rngTable = Get(RngOffset, RngSize).Chunk(1).ToList();
			rngTable.Shuffle(rng);

			Put(RngOffset, rngTable.SelectMany(blob => blob.ToBytes()).ToArray());
		}

		public void ExpGoldBoost(double bonus, double multiplier)
		{
			var enemyBlob = Get(EnemyOffset, EnemySize * EnemyCount);
			var enemies = enemyBlob.Chunk(EnemySize);

			foreach (var enemy in enemies)
			{
				var exp = BitConverter.ToUInt16(enemy, 0);
				var gold = BitConverter.ToUInt16(enemy, 2);

				exp += (ushort)(bonus / multiplier);
				gold += (ushort)(bonus / multiplier);

				var expBytes = BitConverter.GetBytes(exp);
				var goldBytes = BitConverter.GetBytes(gold);
				Array.Copy(expBytes, 0, enemy, 0, 2);
				Array.Copy(goldBytes, 0, enemy, 2, 2);
			}

			enemyBlob = Blob.Concat(enemies);

			Put(EnemyOffset, enemyBlob);

			var levelRequirementsBlob = Get(LevelRequirementsOffset, LevelRequirementsSize * LevelRequirementsCount);
			var levelRequirementsBytes = levelRequirementsBlob.Chunk(3).Select(threeBytes => new byte[] { threeBytes[0], threeBytes[1], threeBytes[2], 0 }).ToList();
			for (int i = 0; i < LevelRequirementsCount; i++)
			{
				uint levelRequirement = (uint)(BitConverter.ToUInt32(levelRequirementsBytes[i], 0) / multiplier);
				levelRequirementsBytes[i] = BitConverter.GetBytes(levelRequirement);
			}

			Put(LevelRequirementsOffset, Blob.Concat(levelRequirementsBytes.Select(bytes => (Blob)new byte[] { bytes[0], bytes[1], bytes[2] })));

			// A dirty, ugly, evil piece of code that sets the level requirement for level 2, even though that's already defined in the above table.
			byte firstLevelRequirement = Data[0x3C04B];
			firstLevelRequirement = (byte)(firstLevelRequirement / multiplier);
			Data[0x3C04B] = firstLevelRequirement;
		}

		public static string EncodeFlagsText(UserFlags userFlags)
		{
			var bits = new BigInteger(0);
			
			ApplyUserFlag(ref bits, userFlags.Treasures);
			ApplyUserFlag(ref bits, userFlags.IncentivizeIceCave);
			ApplyUserFlag(ref bits, userFlags.IncentivizeOrdeals);
			ApplyUserFlag(ref bits, userFlags.Shops);
			ApplyUserFlag(ref bits, userFlags.MagicShops);
			ApplyUserFlag(ref bits, userFlags.MagicLevels);
			ApplyUserFlag(ref bits, userFlags.MagicPermissions);
			ApplyUserFlag(ref bits, userFlags.Rng);
			ApplyUserFlag(ref bits, userFlags.EnemyScripts);
			ApplyUserFlag(ref bits, userFlags.EnemySkillsSpells);
			ApplyUserFlag(ref bits, userFlags.EnemyStatusAttacks);
			ApplyUserFlag(ref bits, userFlags.Ordeals);

			ApplyUserFlag(ref bits, userFlags.EarlyRod);
			ApplyUserFlag(ref bits, userFlags.EarlyCanoe);
			ApplyUserFlag(ref bits, userFlags.EarlyOrdeals);
			ApplyUserFlag(ref bits, userFlags.EarlyBridge);
			ApplyFlag(ref bits, userFlags.NoPartyShuffle);
			ApplyFlag(ref bits, userFlags.SpeedHacks);
			ApplyFlag(ref bits, userFlags.IdentifyTreasures);
			ApplyFlag(ref bits, userFlags.Dash);
			ApplyFlag(ref bits, userFlags.BuyTen);

			ApplyFlag(ref bits, userFlags.HouseMPRestoration);
			ApplyFlag(ref bits, userFlags.WeaponStats);
			ApplyFlag(ref bits, userFlags.ChanceToRun);
			ApplyFlag(ref bits, userFlags.SpellBugs);
			ApplyFlag(ref bits, userFlags.EnemyStatusAttackBug);

			ApplyUserFlag(ref bits, userFlags.FunEnemyNames);
			ApplyUserFlag(ref bits, userFlags.PaletteSwap);
			ApplyUserFlag(ref bits, userFlags.TeamSteak);
			ApplyUserFlag(ref bits, userFlags.ShuffleLeader);
			ApplyState(ref bits, (int)userFlags.Music, 4);

			ApplyState(ref bits, SliderToBase64((int)(userFlags.PriceScaleFactor * 10.0)) - 10, 41);
			ApplyState(ref bits, SliderToBase64((int)(userFlags.EnemyScaleFactor * 10.0)) - 10, 41);
			ApplyState(ref bits, SliderToBase64((int)(userFlags.ExpMultiplier * 10.0)) - 10, 41);
			ApplyState(ref bits, SliderToBase64((int)(userFlags.ExpBonus / 10.0)), 51);
			ApplyState(ref bits, SliderToBase64(userFlags.ForcedPartyMembers), 5);

			var bytes = bits.ToByteArray();

			var text = Convert.ToBase64String(bytes);
			text = text.TrimEnd('=');
			text = text.Replace('+', '!');
			text = text.Replace('/', '%');

			return text;
		}

		public static UserFlags DecodeFlagsText(string text)
		{
			var bitString = text.Substring(0, 6);
			bitString = bitString.Replace('!', '+');
			bitString = bitString.Replace('%', '/');
			bitString += "==";

			var bytes = Convert.FromBase64String(bitString);
			var bits = new BigInteger(bytes);

			return new UserFlags
			{
				Treasures = ExtractUserFlag(ref bits),
				IncentivizeIceCave = ExtractUserFlag(ref bits),
				IncentivizeOrdeals = ExtractUserFlag(ref bits),
				Shops = ExtractUserFlag(ref bits),
				MagicShops = ExtractUserFlag(ref bits),
				MagicLevels = ExtractUserFlag(ref bits),
				MagicPermissions = ExtractUserFlag(ref bits),
				Rng = ExtractUserFlag(ref bits),
				EnemyScripts = ExtractUserFlag(ref bits),
				EnemySkillsSpells = ExtractUserFlag(ref bits),
				EnemyStatusAttacks = ExtractUserFlag(ref bits),
				Ordeals = ExtractUserFlag(ref bits),

				EarlyRod = ExtractUserFlag(ref bits),
				EarlyCanoe = ExtractUserFlag(ref bits),
				EarlyOrdeals = ExtractUserFlag(ref bits),
				EarlyBridge = ExtractUserFlag(ref bits),
				NoPartyShuffle = ExtractFlag(ref bits),
				SpeedHacks = ExtractFlag(ref bits),
				IdentifyTreasures = ExtractFlag(ref bits),
				Dash = ExtractFlag(ref bits),
				BuyTen = ExtractFlag(ref bits),

				HouseMPRestoration = ExtractFlag(ref bits),
				WeaponStats = ExtractFlag(ref bits),
				ChanceToRun = ExtractFlag(ref bits),
				SpellBugs = ExtractFlag(ref bits),
				EnemyStatusAttackBug = ExtractFlag(ref bits),

				FunEnemyNames = ExtractUserFlag(ref bits),
				PaletteSwap = ExtractUserFlag(ref bits),
				TeamSteak = ExtractUserFlag(ref bits),
				ShuffleLeader = ExtractUserFlag(ref bits),

				Music = (MusicShuffle)ExtractState(ref bits, 4),

				PriceScaleFactor = (ExtractState(ref bits, 41) + 10) / 10.0,
				EnemyScaleFactor = (ExtractState(ref bits, 41) + 10) / 10.0,
				ExpMultiplier = (ExtractState(ref bits, 41) + 10) / 10.0,
				ExpBonus = ExtractState(ref bits, 51) * 10.0,
				ForcedPartyMembers = ExtractState(ref bits, 5)
			};
		}

		private static void ApplyState(ref BigInteger bits, int value, int range)
		{
			bits *= range;
			bits += value;
		}

		private static void ApplyUserFlag(ref BigInteger bits, UserFlag userFlag)
		{
			ApplyState(ref bits, (int)userFlag, 3);
		}

		private static void ApplyFlag(ref BigInteger bits, bool flag)
		{
			bits = bits << 1;
			if (flag)
			{
				bits = ++bits;
			}
		}

		private static int ExtractState(ref BigInteger bits, int range)
		{
			bits = BigInteger.DivRem(bits, range, out BigInteger remainder);

			return (int)remainder;
		}

		private static UserFlag ExtractUserFlag(ref BigInteger bits)
		{
			return (UserFlag)ExtractState(ref bits, 3);
		}

		private static bool ExtractFlag(ref BigInteger bits)
		{
			return ExtractState(ref bits, 2) == 1;
		}

		private static char SliderToBase64(int value)
		{
			if (value < 0 || value > 63)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be between 0 and 63.");
			}
			else if (value < 26)
			{
				return (char)('A' + value);
			}
			else if (value < 52)
			{
				return (char)('a' + value - 26);
			}
			else if (value < 62)
			{
				return (char)('0' + value - 52);
			}
			else if (value == 62)
			{
				return '!';
			}
			else
			{
				return '%';
			}
		}

		private static int Base64ToSlider(char value)
		{
			if (value >= 'A' && value <= 'Z')
			{
				return value - 'A';
			}
			else if (value >= 'a' && value <= 'z')
			{
				return value - 'a' + 26;
			}
			else if (value >= '0' && value <= '9')
			{
				return value - '0' + 52;
			}
			else if (value == '!')
			{
				return 62;
			}
			else
			{
				return 63;
			}
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RomUtilities;

namespace FF1Lib
{
	public partial class FF1Rom
	{
		public void FixHouse()
		{
			Put(0x03B2CB, Blob.FromHex("20F3ABA91E20E0B2EAEA"));
			Put(0x038816, Blob.FromHex("203B42A4AAACA6FF23A6B23223A7C0059C8A9F8EC5FFFFFFFFFFFFFF"));
		}

		public void FixWeaponStats()
		{
			// Move function pointer
			Put(0x031322, new byte[] { 0xF2 });
			// Move and rewrite function
			Put(0x032CE1, Blob.FromHex("B18248C8B182488AA86891808868918060A9002006ADA9012006ADA9022006ADA9034C06AD8DB3682045A1A000B182A8B93CA0ACB36899A86BADB368A0009180A20220E1ACA00AA20420E1ACA021A20620E1ACA025A20720E1ACA023A20820E1ACA022A20920E1ACA020A20A20E1ACA024A20B20E1ACA901A00B9180A021B1824A4A4A4A4A186901A00C9180A900A00D9180C89180C89180A018B1823007C8C01CD0F7A900297FF02DE9002005AC85888689A002B188A00F9180A005B188A00D9180A004B188A00E9180"));

			// Don't double BB crit
			Put(0x32DDD, new byte[] { 0xEA });

			// Increase crit rate of all weapons
			var weapons = Get(WeaponOffset, WeaponSize * WeaponCount).Chunk(WeaponSize);
			foreach (var weapon in weapons)
			{
				weapon[2] *= 2;
			}
			Put(WeaponOffset, weapons.SelectMany(weapon => weapon.ToBytes()).ToArray());

			// Change damage bonus from +4 to +10
			Put(0x326F5, Blob.FromHex("0A"));

			// Fix player elemental and category defense
			Put(0x325B0, Blob.FromHex("A9008D6D68A00E"));
			Put(0x325E8, Blob.FromHex("A9008D7668A00EA900"));
			Put(0x33618, Blob.FromHex("A900"));
			Put(0x33655, Blob.FromHex("A900"));
		}

		public void FixChanceToRun()
		{
			Put(0x323EF, new byte[] { 0x82 });
		}

		public void FixWarpBug()
		{
			Put(0x3AEF3, Blob.FromHex("187D0063")); // Allows last slot in a spell level to be used outside of battle
		}

		public void FixSpellBugs()
		{
			Put(0x33A4E, Blob.FromHex("F017EA")); // LOCK routine
			Put(0x3029C, Blob.FromHex("0E")); // LOK2 spell effect
			Put(0x302F9, Blob.FromHex("18")); // HEL2 effectivity

			// TMPR and SABR
			// Remove jump to PrepareEnemyMagAttack
			Put(0x334F4, Blob.FromHex("EAEAEA"));
			// Replace PrepareEnemyMagAttack with loading defender strength and hit%
			Put(0x33730, Blob.FromHex("A006B1908D7268A009B1908D8268A005B1908D846860"));
			// Replace end of PreparePlayerMagAttack with saving defender strength and hit%
			Put(0x3369E, Blob.FromHex("60A007AD85689190A009AD82689190A005AD8468919060"));
			// Call new loading code from BtlMag_LoadPlayerDefenderStats
			Put(0x33661, Blob.FromHex("2030B7EAEAEAEA"));
			// Call new saving code from BtlMag_SavePlayerDefenderStats
			Put(0x337C5, Blob.FromHex("209FB6EAEAEAEA"));
			// SABR's hit% bonus
			Put(0x30390, Blob.FromHex("0A"));

			TameExitAndWarpBoss();

			// Cure 4 fix, see 0E_AF7C_Cure4.asm
			Put(0x3AF80, Blob.FromHex("36"));
			Put(0x3AF8F, Blob.FromHex("2AC902F026A564C900F0062061B54CACAFBD0D619D0B61BD0C619D0A612000B4A665DE00632013B64C97AE2026DB4C7CAF0000000000000000000000002000B4A92B202BB9A9008D64004C7CAF"));
			Put(0x3AF13, Blob.FromHex("CC")); // update address for Cure4 routine
		}

		public void FixEnemyStatusAttackBug()
		{
			Put(0x32812, Blob.FromHex("DF")); // This is the craziest freaking patch ever, man.
		}

		public void FixBBAbsorbBug()
		{
			PutInBank(0x0B, 0x9966, Blob.FromHex("2046D860"));
			PutInBank(0x1F, 0xD846, CreateLongJumpTableEntry(0x0F, 0x8800));
		}

		public void FixEnemyElementalResistances()
		{
			// make XFER and other elemental resistance changing spells affect enemies
			// Replace second copy of low bye for hp with elemental resistance
			Put(0x32FE1, Blob.FromHex("13"));
			// Switch to reading elemental resistance from ROM to RAM and make room for the extra byte
			Put(0x3370A, Blob.FromHex("A012B1908D7768A009B1908D7E68B1928D7F68C8B1908D8568C8B1908D82684CFABB00000000"));
			// add JSR to new routine for the extra room
			Put(0x3378C, Blob.FromHex("20B5B6"));
			// move 3 byes from previous subroutine and save elemental resistance of the enemy
			Put(0x336B5, Blob.FromHex("C89190AD7768A01291906000000000000000")); // extra room at the end for new code
		}

		public void FixEnemyAOESpells()
		{
			// Remove comparison and branch on equal which skips the caster when casting aoe spells
			Put(0x33568, Blob.FromHex("EAEAEAEAEAEAEAEA"));
		}

		public void FixVanillaRibbon(Blob[] texts)
		{
			if (texts[(int)Item.Ribbon].Length > 8)
			{
				texts[(int)Item.Ribbon][7] = 0x00;
			}
		}

		public void TameExitAndWarpBoss()
		{
			// See 0E_9C54_ExitBoss.asm for this
			PutInBank(0x0E, 0x9C54, Blob.FromHex("205DB6AD2400D012AD2500F0F32084AD6868A9008D25004C97AE2084ADA9008D2400AE6500DE00636000000000000000000000000000000000000000000020D7CF"));


			Put(0x3B0D6, Blob.FromHex("EAEAEAEAEA20549C")); // Warp
			Put(0x38A77, Blob.FromHex("A02FB3315EAE36B1A8FFA9AFB2B2B50599B8B6ABFF8BFFB7B2FFA4A5B2B5B7")); // Text
			Put(0x38B5A, Blob.FromHex("A0A4B5B3FFA5A4A6AEFFB2B1A805A9AFB2B2B5C099B8B6ABFF8BFFB7B2FFA4A5B2B5B700FFFFFFFFFFFFFF"));

			Put(0x3B0F7, Blob.FromHex("EAEAEAEAEA20549C")); // Exit
			Put(0x38AB3, Blob.FromHex("95B237C5FF972E5D4B26B7C5FF9EB61A1C3005B6B3A84E1B2EA8BB5BC40599B8B6ABFF8BFF2820A53521")); // Text
			Put(0x38BAA, Blob.FromHex("95B2B6B7C5FF97B2FFBAA4BCFFB2B8B7C5059EB6A8FFB7ABACB605B6B3A8AFAFFFB7B2FFA8BBACB7C4FF99B8B6ABFF8BFFB7B2FFA4A5B2B5B7"));
		}
	}
}

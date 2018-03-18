﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;
using NUnit.Framework;

namespace AsyncGenerator.Tests.NUnit.Input
{
	public class AssertThatTryCatch
	{
		public void Test()
		{
			Assert.That(() =>
			{
				SimpleFile.Clear();
				return SimpleFile.Write("");
			}, Throws.Nothing);
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroKLobby
{
    public interface INavigatable
	{
		string PathHead { get; }
        bool TryNavigate(bool reload, params string[] path);
		bool Hilite(HiliteLevel level, params string[] path);
		string GetTooltip(params string[] path);
	}

	public enum HiliteLevel
	{
		None= 0,
		Bold = 1,
		Flash = 2
	}
}

# Vectorize
Vectorize is a free, open source image tracing plug-in for Rhino.

<img width="128" height="128" src="https://github.com/dalefugier/Vectorize/raw/main/Tools/Vectorize.png">

### Overview

The inspiration for Vectorize comes from Gérard Bouteau, a long time Rhino user, trainer, and programmer, who developed a plug-in named [Trace](https://www.food4rhino.com/app/trace) for Rhino 6. Unfortunately for all of us, Gérard passed away unexpectedly in the spring of 2020. Because of this untimely passing, he was never able to port his popular plug-in to Rhino 7 and beyond.

### Details

Vectorize is based off the famous [Potrace](http://potrace.sourceforge.net/) by Peter Selinger, whose application transforms a bitmap into a soft, scalable image made up of vectors. The plug-in uses a C# translation named [CsPotrace](https://www.drawing3d.de/Downloads.aspx) written by Wolfgang Nagl.

### Prerequisites

The following tool is required to build Vectorize :

- [Microsoft Visual Studio](https://visualstudio.microsoft.com/). Visual Studio comes in three editions: Community (free), Professional, and Enterprise. All of these editions will work.

Note, the solution uses the [RhinoCommon](https://www.nuget.org/packages/rhinocommon) package available on [NuGet](https://www.nuget.org/).

### Compiling

1. Clone the repository. At a command prompt, enter the following command:

```
git clone https://github.com/dalefugier/Vectorize
```

2. Open the `Vectorize.sln` solution file in Visual Studio.
3. Press <kbd>F7</kbd>, or click *Build > Build Solution*  to build the solution.

## License

This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 2 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program; if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA. See also http://www.gnu.org/.

See the file [LICENSE](https://github.com/dalefugier/Vectorize/blob/master/LICENSE) for details.

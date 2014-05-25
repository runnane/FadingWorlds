using System;
using System.Windows.Forms;

namespace FadingWorldsClient
{
	internal static class FadingWorldBase {
		[STAThread]
		private static void Main() {
			try {
				Application.Run(new Loader());
			}
			catch (Exception ex) {
				MessageBox.Show(ex.ToString());
			}
		}
	}
}
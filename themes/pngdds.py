# only reason this exists is because Krita doesnt support dds import and export, mby i should make a scuffed extension for it myself

import os, subprocess, sys
from pathlib import Path

def search_dir(path, pathout):
	for name in os.listdir(path):
		path2 = f"{path}/{name}"
		pathout2 = f"{pathout}/{name}"
		if os.path.isfile(path2):
			if path2[-4:] == ".png":
				sys.stdout.write(path2 + "\n")
				sys.stdout.flush()
				
				os.system("convert " + path2 + " -define" + " dds:compression=none " + pathout2[:-3] + "dds")
				# subprocess.Popen(["convert", path2, "-define", "dds:compression=none", pathout2[:-3] + "dds"], shell = True)
		else:
			os.makedirs(pathout2, exist_ok = True)
			search_dir(path2, pathout2)

search_dir("./material_ui_black_plus_pngs", "./material_ui_black_plus")
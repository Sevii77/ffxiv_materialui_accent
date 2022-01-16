# only reason this exists is because Krita doesnt support dds import and export, mby i should make a scuffed extension for it myself

import os, subprocess, sys

def search_dir(path):
	for name in os.listdir(path):
		path2 = f"{path}/{name}"
		if os.path.isfile(path2):
			if path2[-4:] == ".png":
				sys.stdout.write(path2 + "\n")
				sys.stdout.flush()
				
				os.system("convert " + path2 + " -define" + " dds:compression=none " + path2[:-3] + "dds")
				# subprocess.Popen(["convert", path2, "-define", "dds:compression=none", path2[:-3] + "dds"], shell = True)
		else:
			search_dir(path2)

search_dir("./elements_black")
# used to update the material ui themes to remote
import requests, os, sys, json, re, shutil

try:
	shutil.rmtree("./material_ui_black/files/")
except:
	pass

try:
	shutil.rmtree("./material_ui_black_plus/files/")
except:
	pass

# convert pngs to dds
def search_dir(path, pathout):
	for name in os.listdir(path):
		path2 = f"{path}/{name}"
		pathout2 = f"{pathout}/{name}"
		if os.path.isfile(path2):
			if path2[-4:] == ".png":
				sys.stdout.write(path2 + "\n")
				sys.stdout.flush()
				
				os.system("convert " + path2 + " -define" + " dds:compression=none " + pathout2[:-3] + "dds")
		else:
			os.makedirs(pathout2, exist_ok = True)
			search_dir(path2, pathout2)

search_dir("./material_ui_black_plus/pngs", "./material_ui_black_plus/files")

# download mui files
def write_file(path, data):
	pathdir = re.search(r".+/", path).group(0)
	if not os.path.exists(pathdir):
		os.makedirs(pathdir)
		
		with open(path, "wb") as f:
			f.write(data)

mui = json.loads(requests.get("https://api.github.com/repos/skotlex/ffxiv-material-ui/git/trees/master?recursive=1").content)
for node in mui["tree"]:
	if not "4K resolution/Black/Saved/" in node["path"]:
		continue
	
	if not ".dds" in node["path"]:
		continue
	
	gamepath = node["path"].lower()
	gamepath = re.sub("4k resolution/black/saved/ui/", "ui/", gamepath)
	gamepath = re.sub(r"/hud/([a-z0-9_]+)/", r"/uld/\1_hr1/", gamepath)
	gamepath = re.sub(r"/icon/icon/(\d\d\d)(\d\d\d)/", r"/icon/\g<1>000/\1\2_hr1/", gamepath)
	gamepath = re.sub(r"/[a-z0-9_]+_hr1\.dds", "/underlay.dds", gamepath)
	
	sys.stdout.write(gamepath + "\n")
	sys.stdout.flush()
	
	file = requests.get("https://raw.githubusercontent.com/skotlex/ffxiv-material-ui/master/" + node["path"]).content
	write_file("./material_ui_black/files/" + gamepath, file)
	write_file("./material_ui_black_plus/files/" + gamepath, file)
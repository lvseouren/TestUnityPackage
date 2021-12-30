#!/usr/bin/python
# coding=utf-8

import fnmatch, os
import re
import csv

cur_dir = os.path.dirname(os.path.abspath(__file__))
lua_dir = os.path.abspath(cur_dir + "/../Assets/AssetBundles/Lua/")
cs_dir = os.path.abspath(cur_dir + "/../Assets/Script/Modules")
print("lua path", lua_dir)
print("cs path", cs_dir)

will_modify_lua = False
will_modify_cs = False
occur_count = 0
cache_dic = {}

def cache_loc_dic(csv_file):
	global cache_dic
	with open(csv_file, "r", encoding='UTF-8') as csvfile:
		reader = csv.reader(csvfile, delimiter = ',')
		i = 0
		for row in reader:
			i += 1
			if not row or i == 1:
				continue
			key = row[0]
			chs = row[1]
			# cache_dic[key] = chs
			cache_dic[chs] = key

def replaceI2Loc(matchobj):
	global occur_count
	occur_count = occur_count + 1
	origin = matchobj.group(0)
	key = matchobj.group(1)
	replacement = ''
	if key in cache_dic:
		replacement = '"' + cache_dic[key] +'"'
		replacement = "PgLocalizeUtil.GetLocalizeString(" + replacement + ")"
	else:
		replacement = '"' + key + '"'
	replacement = replacement.replace("\n", "\\n")
	replacement = replacement.replace("`", ",")

	print(origin, "=>", replacement)
	return replacement
		
if __name__ == '__main__':
	# cache_loc_dic("../Assets/Localization/CSV/OneForAll.csv")

	print("Start to replace all IL2Str in Lua.")
	for root, dirnames, filenames in os.walk(lua_dir):
		for filename in fnmatch.filter(filenames, '*.lua'):
			file_path = os.path.join(root, filename)
			with open(file_path, "r+", encoding='utf-8') as lua:
				content = lua.read()
				content_new = re.sub(r'"([\u4e00-\u9fa5]*?)"', replaceI2Loc, content)
				if will_modify_lua:
					lua.seek(0)
					lua.truncate()
					lua.write(content_new)
	print("Done! Replace {} IL2Str.".format(occur_count))

	occur_count = 0
	print("Start to replace all LocalizationManager.GetTranslation in CS.")
	for root, dirnames, filenames in os.walk(cs_dir):
		for filename in fnmatch.filter(filenames, '*.cs'):
			file_path = os.path.join(root, filename)
			print(file_path)
			with open(file_path, "r+", encoding='utf-8') as cs:
				try:
					content = cs.read()
					content_new = re.sub(r'"([.!?\u3000-\u303f\ufb00-\ufffd\u3002\uff1b\uff0c\uff1a\u201c\u201d\uff08\uff09\u3001\uff1f\u300a\u300b\u4e00-\u9fa5]*?)"', replaceI2Loc, content)
					if will_modify_cs:
						cs.seek(0)
						cs.truncate()
						cs.write(content_new)
				except Exception as e:
					print(e)
	print("Done! Replace {} GetTranslation.".format(occur_count))
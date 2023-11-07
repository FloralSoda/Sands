#!/bin/bash
$ret = 0
$msg = "Enter number next to function: "
input () {
	while :; do
		read -p $msg ret
		[[ $ret =~ ^[0-9]+$ ]] || { echo "Enter a valid number"; continue; }
		if ((ret >= 1 && ret <= 2)); then
			break
		else
			echo "Input out of range, please try again"
		fi
	done
}

clear

if [ ! -d ./assets ]; then
	mkdir -p ./assets;
fi
if [ ! -d ./assets/audio ]; then
	mkdir -p ./assets/audio;
fi
if [ ! -d ./assets/models ]; then
	mkdir -p ./assets/models;
fi
if [ ! -d ./assets/textures ]; then
	mkdir -p ./assets/textures;
fi

echo "1. Pack"
echo "2. Unpack"

$msg = "Enter the value next to function: "
input()
$op = $ret

echo "1. Audio"
echo "2. Models"
echo "3. Textures"
echo "4. All"
echo "5. Models and Textures"

$msg = "Enter number next to media type: "
input()
$media = $ret

if [$op -eq 2]; then

	$msg = "Should these files overwrite the current files? (Y Yes, N No, A Ask): " c
	if ["$c" = "Y"]; then
		ow = "-o+"
	elif ["$c" = "N"]; then
		ow = "-o-"
	elif ["$c" = "A"]; then
		ow = "-o"
	fi
	
	if [ $media -eq 5 ]; then
		echo "Unpacking Models"
		echo "--------------------------"
		unrar x $ow ./audio.assets ./assets/audio
		echo "--------------------------"
	fi
  
	if [ $media -eq 4 ]; then
		echo "Unpacking Models"
		echo "--------------------------"
		unrar x $ow ./models.assets ./assets/models
		echo "--------------------------"
	fi
  
	if [ $media -eq 3 ]; then
		echo "Unpacking Textures"
		echo "--------------------------"
		unrar x $ow ./textures.assets ./assets/textures
		echo "--------------------------"
	fi
  
	if [ $media -eq 2 ]; then
		echo "Unpacking Models"
		echo "--------------------------"
		unrar x $ow ./models.assets ./assets/models
		echo "--------------------------"
	fi
  
	if [ $media -eq 1 ]; then
		echo "Unpacking Audio"
		echo "--------------------------"
		unrar x $ow ./audio.assets ./assets/audio
		echo "--------------------------"
	fi
	
elif [$op -eq 1]; then
	if [ $media -eq 5 ]; then
		echo "Packing Models"
		echo "--------------------------"
		cd ./assets/models
		rar a ../../models.assets .
		echo "--------------------------"
		cd ../..
	fi
  
	if [ $media -eq 4 ]; then
		echo "Packing Audio"
		echo "--------------------------"
		cd ./assets/audio
		rar a ../../audio.assets .
		echo "--------------------------"
		cd ../..
	fi
  
	if [ $media -eq 3 ]; then
		echo "Packing Textures"
		echo "--------------------------"
		cd ./assets/textures
		rar a ../../textures.assets .
		echo "--------------------------"
		cd ../..
	fi
  
	if [ $media -eq 2 ]; then
		echo "Packing Models"
		echo "--------------------------"
		cd ./assets/models
		rar a ../../models.assets .
		echo "--------------------------"
		cd ../..
	fi
  
	if [ $media -eq 1 ]; then
		echo "Packing Audio"
		echo "--------------------------"
		cd ./assets/audio
		rar a ../../audio.assets .
		echo "--------------------------"
		cd ../..
	fi
fi

echo "Success."
read -p "Press any key to continue..."
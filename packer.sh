#!/bin/bash
ret=0
msg="Enter number next to function: "
upper=2
lower=1
get_input () {
	while :; do
		read -p "$msg" ret
		[[ $ret =~ ^[0-9]+$ ]] || { echo "Enter a valid number"; continue; }
		if ((ret >= $lower && ret <= $upper)); then
			break
		else
			echo "Input out of range, please try again"
		fi
	done
}
get_yn() {
	while :; do
		read -p "$msg" yn

		case $yn in 
			[yY] )
				c="Y"
				break;;
			[nN] )
				c="N"
				break;;
			[aA] )
				c="A"
				break;;
			* ) echo invalid response;;
		esac
	done
}

clear

if [[ ! -d ./assets ]]; then
	mkdir -p ./assets;
fi
if [[ ! -d ./assets/audio ]]; then
	mkdir -p ./assets/audio;
fi
if [[ ! -d ./assets/models ]]; then
	mkdir -p ./assets/models;
fi
if [[ ! -d ./assets/textures ]]; then
	mkdir -p ./assets/textures;
fi

echo "1. Pack"
echo "2. Unpack"

msg="Enter the value next to function: "
get_input
oper=$ret

echo "1. Audio"
echo "2. Models"
echo "3. Textures"
echo "4. All"
echo "5. Models and Textures"

upper=5
msg="Enter number next to media type: "
get_input
media=$ret

if [[ $oper -eq 2 ]]; then

	msg="Should these files overwrite the current files? (Y Yes, N No, A Ask): "
	get_yn
	if [[ "$c" = "Y" ]]; then
		ow="-o+"
	elif [[ "$c" = "N" ]]; then
		ow="-o-"
	elif [[ "$c" = "A" ]]; then
		ow="-o"
	fi
  
	if [ $media -eq 3 ] || [ $media -eq 5 ] || [ $media -eq 4 ]; then
		echo "Unpacking Textures"
		echo "--------------------------"
		unrar x $ow ./textures.assets ./assets/textures
		echo "--------------------------"
	fi
  
	if [ $media -eq 2 ] || [ $media -eq 5 ] || [ $media -eq 4 ]; then
		echo "Unpacking Models"
		echo "--------------------------"
		unrar x $ow ./models.assets ./assets/models
		echo "--------------------------"
	fi
  
	if [ $media -eq 1 ] || [ $media -eq 5 ]; then
		echo "Unpacking Audio"
		echo "--------------------------"
		unrar x $ow ./audio.assets ./assets/audio
		echo "--------------------------"
	fi
	
elif [[ $oper -eq 1 ]]; then
	if [ $media -eq 3 ] || [ $media -eq 5 ] || [ $media -eq 4 ]; then
		echo "Packing Textures"
		echo "--------------------------"
		cd ./assets/textures
		rar a ../../textures.assets .
		echo "--------------------------"
		cd ../..
	fi
  
	if [ $media -eq 2 ] || [ $media -eq 5 ] || [ $media -eq 4 ]; then
		echo "Packing Models"
		echo "--------------------------"
		cd ./assets/models
		rar a ../../models.assets .
		echo "--------------------------"
		cd ../..
	fi
  
	if [ $media -eq 1 ] || [ $media -eq 5 ]; then
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

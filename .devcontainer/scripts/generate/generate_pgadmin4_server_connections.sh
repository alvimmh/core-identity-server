#!/bin/bash

# get current file's absolute path
script_file_path=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)

source_file="$script_file_path/../../../src/CoreIdentityServer/appsettings.Development.json"
target_file="$script_file_path/../../servers.json"

# json sections for generating the restructured json
target_section_keys=("cis_main_database" "cis_auxiliary_database")

# check if source file exists
if [ ! -f "$source_file" ]; then
    echo "Error: Source file '$source_file' not found."
    exit 1
fi

# load the json object using jq and the prefix param and output it
read_json() {
    local json_source="$1"
    local prefix="$2"

    echo $(jq ".$prefix" "$json_source")
}

# read from loaded json object and restructure it to desired format
restructure_json() {
    local json_source="$1"

    # declaring an associative array with keys following restructured data format
    # and values following restructured data type
    declare -A server_object_keys=(
        ["Name"]="string"
        ["Group"]="string"
        ["Port"]="integer"
        ["Username"]="string"
        ["Host"]="string"
        ["SSLMode"]="string"
        ["MaintenanceDB"]="string"
    )

    # extract keys from the JSON source and convert it to a proper bash array
    local keys=($(jq -r 'keys_unsorted[]' <<< "$json_source"))

    local total_keys=${#keys[@]}
    local total_server_object_keys=${#server_object_keys[@]}

    local json_object=""
    local error_message=""
 
    # loop over the keys and store the restructured content to json_object
    for key in "${keys[@]}"; do      
        local key_match_positive=false
        
        local corresponding_server_object_key=""
        local corresponding_server_object_key_type=""

        # loop over server_object_keys to filter required keys
        for server_object_key in "${!server_object_keys[@]}"; do
            # convert the key to lowercase and remove non-alphanumeric characters
            key_content=$(echo "$key" | tr '[:upper:]' '[:lower:]' | tr -cd '[:alnum:]')

            # convert the server_object_key to lowercase and remove non-alphanumeric characters
            server_object_key_content=$(echo "$server_object_key" | tr '[:upper:]' '[:lower:]' | tr -cd '[:alnum:]')
            
            # check if contents of both strings are same
            if [[ "$key_content" == "$server_object_key_content" ]]; then
                key_match_positive=true
                corresponding_server_object_key="$server_object_key"
                corresponding_server_object_key_type="${server_object_keys["$server_object_key"]}"
                break
            fi
        done

        if [ $key_match_positive = false ]; then
            # key does not match any element in server_object_keys
            # proceed to the next key
            continue
        fi

        local value=$(jq -r ".$key" <<< "$json_source" | sed 's/^[ ]*//;s/[ ]*$//')

        # check if value is not empty
        if [ -n "$value" ]; then
            # add json file indentation, 3 tabs for 3 layer deep
            # 1st layer { (curly brace) for json start
            # 2nd layer "Servers" section
            # 3rd layer server connection keys, e.g., "1", "2", "3"
            local key_value=$'\t\t\t'
            
            # remove non numeric characters
            numeric_content_of_value=$(echo "$value" | tr -cd '[:digit:]')

            # check if value should be enclosed in double quotes to make it a string
            # or not enclosed with quotes to make it an integer
            if [[ "$numeric_content_of_value" == "$value" && "$corresponding_server_object_key_type" == "integer" ]]; then
                key_value+="\"${corresponding_server_object_key}\": $value"
            else
                key_value+="\"${corresponding_server_object_key}\": \"$value\""
            fi

            # add key_value in a new line if json_object already has data
            if [ -n "$json_object" ]; then
                json_object+=$',\n'"$key_value"
            else
                json_object="$key_value"
            fi
        else
            # error message to pinpoint the key-value in the json source file
            error_message="Error: The value for key '$key' in section '$2' is empty."
            break
        fi
    done

    if [ -n "$error_message" ]; then
        echo "$error_message"
        return 1
    else
        echo -e "$json_object"
    fi
}

# function that uses the read_json and parse_json file and outputs
# the restructured content for a json section
generate_restructured_json()
{
    local json_source="$1"
    local json_section_key="$2"

    local json_section=$(read_json "$json_source" "$json_section_key")

    generated_variables=$(restructure_json "$json_section" "$json_section_key")

    if [ $? -ne 0 ]; then
        echo "$generated_variables"
        return 1
    else
        echo "$generated_variables"
    fi
}

# for the given sections, this function iterates through them,
# reads the json data, restructures it and writes it to a target file.
#
# if there is any error, the function doesn't write anyting to
# the target file.
write_restructured_json()
{
    local json_source="$1"
    local destination_file="$2"

    # need to shift the the first two params to access the array
    shift 2
    local section_keys=("$@")

    output=""
    error=0
    counter=0

    local index
    local total_section_keys=${#section_keys[@]}

    # loops over json section keys and generates restructured json content
    for ((index = 1; index <= total_section_keys; index++)); do
        local section_key_index=index-1
        local section=${section_keys[section_key_index]}
        
        result=$(generate_restructured_json "$json_source" "$section")

        if [ $? -ne 0 ]; then
            error=1
            break
        else
            if [ -n "$output" ]; then
                # add server connection section when the current for loop iteration
                # is not the first one
                output+=$'\n\t\t'"\"$index\": {"$'\n'
                output+="$result"$'\n'
            else
                # add opening brace {
                output+=$'{\n'

                # add first section "Servers"
                output+=$'\t"Servers": {\n'

                # add first server connection section key (integer, e.g., "1", "2", "3")
                output+=$'\t\t'"\"$index\": {"$'\n'

                # add server connection section 
                output+="$result"$'\n'
            fi

            # close server connection section
            output+=$'\t\t}'

            # add comma if the current for loop iteration is not the last one
            if [[ $index -lt $total_section_keys ]]; then
                output+=","
            fi
        fi
    done

    if [ $error -ne 0 ]; then
        echo "$result"
        echo "Error: Failed to convert JSON data and write to '$destination_file'."
    else
        # close "Servers" section
        output+=$'\n\t}'

        # close json file
        output+=$'\n}'
        
        # check if target file already exist, give warning and clear its contents
        if [ -f "$destination_file" ]; then
            echo "Warning: '$destination_file' file already exists. Overwriting its contents."
            truncate -s 0 "$destination_file"
        fi

        echo "$output" >> "$destination_file"
        echo "JSON sections restructured and written to '$destination_file' successfully."
    fi
}

# calls the write_restructured_json function and gives it required params
write_restructured_json "$source_file" "$target_file" "${target_section_keys[@]}"

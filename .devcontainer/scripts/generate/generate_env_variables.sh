#!/bin/bash

script_file_path=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)

source_file="$script_file_path/../../../src/CoreIdentityServer/appsettings.Development.json"
target_file="$script_file_path/../../.env"
target_section_keys=("cis_main_database" "cis_auxiliary_database" "pgadmin4")
keywords_for_ignoring_keys=("password")

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

# read from loaded json object and parse it to desired format
parse_json() {
    local json_source="$1"
    local prefix="$2"
    
    # capitalize prefix
    prefix=$(tr '[:lower:]' '[:upper:]' <<< "$prefix")

    # extract keys from the JSON object
    local keys=$(jq -r 'keys_unsorted[]' <<< "$json_source")

    local env_variables=""
    local error_message=""
 
    # loop over the keys and store the parsed content to env_variables
    for key in $keys; do
        local need_to_ignore_key=false

        for ignore_keyword in $keywords_for_ignoring_keys; do
            if [[ $key == *$ignore_keyword* ]]; then
                need_to_ignore_key=true
                break
            fi
        done

        if [ $need_to_ignore_key = true ]; then
            continue
        fi

        local value=$(jq -r ".$key" <<< "$json_source" | sed 's/^[ ]*//;s/[ ]*$//')

        # check if value is not empty
        if [ -n "$value" ]; then
            local key_value="${prefix}_${key^^}='$value'"
            
            # add key_value in a new line if env_variables already has data
            if [ -n "$env_variables" ]; then
                env_variables+=$'\n'"$key_value"
            else
                env_variables="$key_value"
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
        echo -e "$env_variables"
    fi
}

# function that uses the read_json and parse_json file and outputs
# the parsed content for a json section
generate_env_variables()
{
    local json_source="$1"
    local json_section_key="$2"

    local json_section=$(read_json "$json_source" "$json_section_key")

    generated_variables=$(parse_json "$json_section" "$json_section_key")

    if [ $? -ne 0 ]; then
        echo "$generated_variables"
        return 1
    else
        echo "$generated_variables"
    fi
}


# for the given sections, this function iterates through them,
# reads the json data, parses it and writes it to a target file.
#
# if there is any error, the function doesn't write anyting to
# the target file.
write_env_variables()
{
    local json_source="$1"
    local destination_file="$2"

    # need to shift the the first two params to access the array
    shift 2
    local section_keys=("$@")

    output=""
    error=0
    
    for section in ${section_keys[@]}; do
        result=$(generate_env_variables "$json_source" "$section")

        if [ $? -ne 0 ]; then
            error=1
            break
        else
            if [ -n "$output" ]; then
                output+=$'\n'"$result"
            else
                output="$result"
            fi
        fi
    done

    if [ $error -ne 0 ]; then
        echo "$result"
        echo "Error: Failed to convert JSON data and write to '$destination_file'."
    else
        # check if target file already exist, give warning and clear its contents
        if [ -f "$destination_file" ]; then
            echo "Warning: '$destination_file' file already exists. Overwriting its contents."
            truncate -s 0 "$destination_file"
        fi

        echo "$output" >> "$destination_file"
        echo "JSON sections converted and written to '$destination_file' successfully."
    fi
}

# calls the write_env_variables function and gives it required params
write_env_variables "$source_file" "$target_file" "${target_section_keys[@]}"

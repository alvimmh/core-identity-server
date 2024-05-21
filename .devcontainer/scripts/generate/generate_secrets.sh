#!/bin/bash

script_file_path=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)

source_file="$script_file_path/../../../src/CoreIdentityServer/appsettings.Development.json"
target_directory="$script_file_path/../../secrets"
target_section_keys=("cis_main_database" "cis_auxiliary_database" "pgadmin4")
keywords_for_selecting_keys=("password")

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
parse_json_and_create_secret() {
    local json_source="$1"
    local prefix="$2"
    local destination_directory="$3"
    
    # capitalize prefix
    prefix=$(tr '[:lower:]' '[:upper:]' <<< "$prefix")

    # extract keys from the JSON object
    local keys=$(jq -r 'keys_unsorted[]' <<< "$json_source")

    local message=""
    local encountered_error=0
 
    # loop over the keys and store the parsed content to env_variables
    for key in $keys; do
        local need_to_select_key=false

        for select_keyword in "${keywords_for_selecting_keys[@]}"; do
            if [[ $key == *"$select_keyword"* ]]; then
                need_to_select_key=true
            fi
        done

        if [ $need_to_select_key = false ]; then
            continue
        fi

        local value=$(jq -r ".$key" <<< "$json_source" | sed 's/^[ ]*//;s/[ ]*$//')

        # check if value is not empty
        if [ -n "$value" ]; then
            local secret_file_name="${prefix}_${key^^}.txt"
            local destination_file="$destination_directory/$secret_file_name"

            # check if target file already exist, give warning and clear its contents
            if [ -f "$destination_file" ]; then
                echo "Warning: '$destination_file' file already exists. Overwriting its contents."
                
                # as a security measure remove all content from the secret file
                truncate -s 0 "$destination_file"
            fi

            # write to secret file, becareful not to add newline characters
            echo -n "$value" > "$destination_file"

            # create new line if this is not the first iteration
            if [ -n "$message" ]; then
                message+=$'\n'
            fi
            
            message+="Success: Secret file "$destination_file" created"
        else
            # error message to pinpoint the key-value in the json source file
            message+="Error: The value for key '$key' in section '$2' is empty."
            encountered_error=1
            break
        fi
    done

    if [ $encountered_error -ne 0 ]; then
        echo -e "$message"
        return 1
    else
        echo -e "$message"
    fi
}


# function that uses the read_json and parse_json file and outputs
# the parsed content for a json section
generate_secret_files()
{
    local json_source="$1"
    local json_section_key="$2"
    local destination_directory="$3"

    local json_section=$(read_json "$json_source" "$json_section_key")

    local output_message=$(parse_json_and_create_secret "$json_section" "$json_section_key" "$destination_directory")

    if [ $? -ne 0 ]; then
        echo "$output_message"
        return 1
    else
        echo "$output_message"
    fi
}


# for the given sections, this function iterates through them,
# reads the json data, parses it and writes it to a target file.
#
# if there is any error, the function doesn't write anyting to
# the target file.
create_docker_secrets()
{
    local json_source="$1"
    local destination_directory="$2"

    # need to shift the the first two params to access the array
    shift 2
    local section_keys=("$@")

    output=""
    error=0

    for section in ${section_keys[@]}; do
        output=$(generate_secret_files "$json_source" "$section" "$destination_directory")

        if [ $? -ne 0 ]; then
            echo "$output"
            error=1
            break
        else
            echo "$output"
        fi
    done

    if [ $error -ne 0 ]; then
        echo "Error: aborting creation of further secrets."
    else
        echo "Secrets written to '$target_directory' successfully."
    fi
}

# calls the create_docker_secrets function and gives it required params
create_docker_secrets "$source_file" "$target_directory" "${target_section_keys[@]}"

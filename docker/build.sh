#!/bin/bash

while getopts 'spr:' flag;
do
  case "${flag}" in
    s) stable="true"
       echo "Building with stable tag" ;;
    p) push="true"
       echo "Pushing after build";;
    r) IFS="," read -ra additional_registries <<< "${OPTARG}"
       echo "Additionnal registries: ${additional_registries[@]}";;
    *) echo "Unknown parameter passed: $1"; exit 1 ;;
  esac
done

LATEST_TAG=latest
STABLE_TAG=stable

export REGISTRY=${REGISTRY:-gcr.io/csb-anthos}
export API_REPOSITORY=auth/directory/api
export API_TAG=$LATEST_TAG
echo "Building images with the default '$LATEST_TAG' tag."
docker-compose -f docker-compose-build.yaml build

if [ "$stable" == "true" ]
  then
    echo "Tagging images with the the 'stable' tag."
    docker tag $REGISTRY/$API_REPOSITORY:$LATEST_TAG $REGISTRY/$API_REPOSITORY:$STABLE_TAG
fi

dotnet tool restore --tool-manifest ../.config/dotnet-tools.json
export API_VERSION=$(dotnet version -p ../src/Csb.Directory.Api/Csb.Directory.Api.csproj --show | awk '{print $3}')
echo "Tagging images with their version tag."
docker tag $REGISTRY/$API_REPOSITORY:$LATEST_TAG $REGISTRY/$API_REPOSITORY:$API_VERSION

if [ "${#additional_registries[@]}" -gt "0" ]
  then
    echo "Tagging images with the additional registries."
    for additional_registry in "${additional_registries[@]}"; do
      docker tag $REGISTRY/$API_REPOSITORY:$LATEST_TAG $additional_registry/$API_REPOSITORY:$LATEST_TAG
      if [ "$stable" == "true" ]
        then
          docker tag $REGISTRY/$API_REPOSITORY:$LATEST_TAG $additional_registry/$API_REPOSITORY:$STABLE_TAG
      fi
      docker tag $REGISTRY/$API_REPOSITORY:$LATEST_TAG $additional_registry/$API_REPOSITORY:$API_VERSION
    done
fi

if [ "$push" == "true" ]
then
  echo "Pushing images."
  docker push $REGISTRY/$API_REPOSITORY --all-tags
  if [ "${#additional_registries[@]}" -gt "0" ]
    then
      echo "Pushing images with the additional registries."
      for additional_registry in "${additional_registries[@]}"; do
        docker push $additional_registry/$API_REPOSITORY --all-tags
      done
  fi
fi
#!/bin/sh

if [ $# -ne 6 ]; then
    echo "Usage:  ./configure-ec2.sh [bucket] [cidr ingress] [image id] [instance type] [azure notification hub] [azure notification hub full access signature]"
    echo "\t[bucket]:  Bucket containing data"
    echo "\t[cidr ingress]:  SSH ingress range, in CIDR format (e.g., 123.456.0.0/16)"
    echo "\t[image id]:  Image ID to use (e.g., ami-a4c7edb2)"
    echo "\t[instance type]:  Instance type (e.g., t2.micro)"
    echo "\t[azure notification hub]:  The Azure notification hub URL (https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages)"
    echo "\t[azure notification hub full access signature]:  The Azure notification hub full access signature (DefaultFullSharedAccessSignature)
    exit 1
fi

bucket=$1

########################
##### EC2 instance #####
########################

# create key pair and secure it
keyPairName=$bucket
pemFileName="${keyPairName}.pem"
aws ec2 create-key-pair --key-name $keyPairName  --query 'KeyMaterial' --output text > $pemFileName
chmod 400 $pemFileName

# create security group with SSH inbound rule
securityGroupName=$bucket
aws ec2 create-security-group --group-name $securityGroupName --description "$securityGroupName Security Group"
aws ec2 authorize-security-group-ingress --group-name $securityGroupName --protocol tcp --port 22 --cidr $2

# launch ec2 instance and wait for it to start
instanceId=$(aws ec2 run-instances --image-id $3 --count 1 --instance-type $4 --key-name $keyPairName --security-groups $securityGroupName | jq -r .Instances[0].InstanceId)
aws ec2 wait instance-running --instance-ids $instanceId
sleep 15
aws ec2 create-tags --resources $instanceId --tags Key=Name,Value=$bucket
publicIP=$(aws ec2 describe-instances --instance-ids $instanceId --query "Reservations[*].Instances[*].PublicIpAddress" --output=text)

###################################################################################
##### Read-only IAM group/user:  enables someone to read data from the bucket #####
###################################################################################

# create group
echo "Creating read-only IAM group..."
iamReadOnlyGroupName="${bucket}-ro-group"
aws iam create-group --group-name $iamReadOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to create read-only IAM group."
    exit $?
fi

# create/put group policy
echo "Attaching read-only IAM group policy..."
cp ./iam-read-only-group-policy.json tmp.json
sed "s/bucketName/$bucket/" ./tmp.json > ./tmp2.json
mv tmp2.json tmp.json
aws iam put-group-policy --group-name $iamReadOnlyGroupName --policy-document file://tmp.json --policy-name "${iamReadOnlyGroupName}-policy"
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to put IAM read-only group policy."
    exit $?
fi
rm tmp.json

# create read-only IAM user
echo "Creating read-only IAM user..."
iamReadOnlyUserName="${bucket}-ro-user"
aws iam create-user --user-name $iamReadOnlyUserName
if [ $? -ne 0 ]; then
    echo "Failed to create read-only IAM user."
    exit $?
fi

# create access key for user
echo "Creating access key for read-only IAM user..."
iamAccessKeyJSON=$(aws iam create-access-key --user-name $iamReadOnlyUserName)
iamAccessKeyID=$(echo $iamAccessKeyJSON | jq -r .AccessKey.AccessKeyId)
iamAccessKeySecret=$(echo $iamAccessKeyJSON | jq -r .AccessKey.SecretAccessKey)

# add user to group
echo "Adding read-only IAM user to read-only IAM group..."
aws iam add-user-to-group --user-name $iamReadOnlyUserName --group-name $iamReadOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to add read-only IAM user to read-only group."
    exit $?
fi

##################################
##### Instance configuration #####
##################################

# upload IAM credentials to EC2 instance
cp credentials tmp
sed "s/keyId/$iamAccessKeyID/" tmp > tmp2
mv tmp2 tmp
sed "s#keySecret#$iamAccessKeySecret#" tmp > tmp2
mv tmp2 tmp
ssh -i $pemFileName ec2-user@$publicIP "mkdir .aws"
scp -i $pemFileName tmp ec2-user@$publicIP:~/.aws/credentials
rm tmp

# set up push notification backend

# edit/upload get-sas script
sed "s/XXXXURLXXXX/$5" get-sas.js > tmp
sed "s/XXXXKEYXXXX/$6" tmp > tmp2
scp -i $pemFileName tmp2 ec2-user@$publicIP:~/get-sas.js
rm tmp tmp2

# upload push notification processor and supporting tools
scp -i $pemFileName send-push-notifications.sh ec2-user@$publicIP:~/
ssh -i $pemFileName ec2-user@$publicIP "chmod +x send-push-notifications.sh"
ssh -i $pemFileName ec2-user@$publicIP "sudo yum install jq"
ssh -i $pemFileName ec2-user@$publicIP "curl -o- https://raw.githubusercontent.com/creationix/nvm/v0.33.8/install.sh | bash && . ~/.nvm/nvm.sh && nvm install 8.11.2"

# configure crontab to run push notification processor
sed "s/BUCKET/$bucket" push-notification-crontab > tmp
scp -i $pemFileName tmp ec2-user@$publicIP:~/push-notification-crontab
ssh -i $pemFileName ec2-user@$publicIP "crontab push-notification-crontab"
rm tmp

# install other linux packages
echo "Installing packages..."
ssh -i $pemFileName ec2-user@$publicIP "sudo yum -y install R libpng-devel libjpeg-turbo-devel openssl-devel emacs"

# install SensusR package
echo "Installing SensusR..."
ssh -i $pemFileName ec2-user@$publicIP "sudo R -e 'install.packages(\"SensusR\", repos = \"http://mirrors.nics.utk.edu/cran/\")'"

# done
echo "EC2 instance is ready at $publicIP using private PEM file ${pemFileName}."

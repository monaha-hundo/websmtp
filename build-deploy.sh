cd websmtp
dotnet publish --arch amd64 --os linux -c Release -o ../build
cp ../run.sh ../build
rsync -av ../build/ 104.254.181.80:~/build
# Santa API

## Usage:

### Post santa your list
Example:

POST /Simon/ 

Body:
```json
{
	"list": [ "Sonos", "F#" ]
}
```

### Show your list to people
Example:

GET /Simon/

Response:
```json
{
    "Message": "Hohoho, here you go",
    "Result": [
        "Sonos",
        "F#"
    ]
}
```

### Get amazon links for your list

GET /Simon/amazon

```json
{
    "Message": "Hohoho, here is what they want",
    "Result": [
        "http://www.amazon.co.uk/Sonos-PLAY-Black-Wireless-Hi-Fi/dp/B00FMS1KO0%3FSubscriptionId%3DAKIAIIQ3YASKHW4677JQ%26tag%3Dsimonhdickson-20%26linkCode%3Dxm2%26camp%3D2025%26creative%3D165953%26creativeASIN%3DB00FMS1KO0",
        "http://www.amazon.co.uk/Expert-3-0-3rd-Edition-Apress/dp/1430246502%3FSubscriptionId%3DAKIAIIQ3YASKHW4677JQ%26tag%3Dsimonhdickson-20%26linkCode%3Dxm2%26camp%3D2025%26creative%3D165953%26creativeASIN%3D1430246502"
    ]
}
```
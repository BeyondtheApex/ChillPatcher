package crypto

import (
	"crypto/md5"
	"encoding/hex"
	"fmt"
	"math/rand"
	"sort"
	"strings"
	"time"
)

// SignParams signs the API request parameters
// QQ Music uses a specific signing algorithm for API authentication
func SignParams(params map[string]string) string {
	// Sort keys alphabetically
	keys := make([]string, 0, len(params))
	for k := range params {
		keys = append(keys, k)
	}
	sort.Strings(keys)

	// Build query string
	var parts []string
	for _, k := range keys {
		if params[k] != "" {
			parts = append(parts, fmt.Sprintf("%s=%s", k, params[k]))
		}
	}
	queryStr := strings.Join(parts, "&")

	// Calculate MD5 hash
	hash := md5.Sum([]byte(queryStr))
	return hex.EncodeToString(hash[:])
}

// GenerateGUID generates a random GUID for QQ Music requests
func GenerateGUID() string {
	rand.Seed(time.Now().UnixNano())
	return fmt.Sprintf("%d", rand.Int63n(9999999999)+1000000000)
}

// GenerateSearchID generates a search ID
func GenerateSearchID() string {
	rand.Seed(time.Now().UnixNano())
	return fmt.Sprintf("%d", rand.Int63n(99999999999999)+10000000000000)
}

// GetTimestamp returns the current Unix timestamp
func GetTimestamp() int64 {
	return time.Now().Unix()
}

// GetMilliTimestamp returns the current Unix timestamp in milliseconds
func GetMilliTimestamp() int64 {
	return time.Now().UnixMilli()
}

// GenerateSecuritySign generates the sign parameter for QQ Music API
// Format: zza + randomString(10-16 chars) + MD5("CJBPACrRuNy7" + data)
func GenerateSecuritySign(jsonData string) string {
	// Generate random string (10-16 lowercase letters and digits)
	const charset = "abcdefghijklmnopqrstuvwxyz0123456789"
	rand.Seed(time.Now().UnixNano())
	length := rand.Intn(7) + 10 // 10-16 chars

	randomPart := make([]byte, length)
	for i := range randomPart {
		randomPart[i] = charset[rand.Intn(len(charset))]
	}

	// Calculate MD5 of "CJBPACrRuNy7" + jsonData
	toHash := "CJBPACrRuNy7" + jsonData
	hash := md5.Sum([]byte(toHash))
	hashStr := hex.EncodeToString(hash[:])

	// Combine: "zza" + randomString + md5hash
	return "zza" + string(randomPart) + hashStr
}

// ZZASign generates the ZZA sign parameter used in some QQ Music APIs
// This is a simplified implementation
func ZZASign(data string) string {
	// The actual ZZA signing is complex and involves multiple steps
	// This is a placeholder that returns a basic sign
	hash := md5.Sum([]byte(data + "zzaqqmusic"))
	return hex.EncodeToString(hash[:])
}

// ComputeSignature computes the sign parameter for QQ Music API
// The sign algorithm involves MD5 hashing of sorted parameters
func ComputeSignature(method string, params map[string]interface{}) string {
	// Build signature string
	var builder strings.Builder
	builder.WriteString("CJBPACrRuNy7")

	// Sort and append params
	keys := make([]string, 0, len(params))
	for k := range params {
		keys = append(keys, k)
	}
	sort.Strings(keys)

	for _, k := range keys {
		builder.WriteString(fmt.Sprintf("%s=%v", k, params[k]))
	}

	builder.WriteString("CJBPACrRuNy7")

	// MD5 hash
	hash := md5.Sum([]byte(builder.String()))
	return strings.ToUpper(hex.EncodeToString(hash[:]))
}

// GenerateCommData generates common request data for QQ Music API
func GenerateCommData(uin int64) map[string]interface{} {
	return map[string]interface{}{
		"g_tk":             5381,
		"loginUin":         uin,
		"hostUin":          0,
		"format":           "json",
		"inCharset":        "utf8",
		"outCharset":       "utf-8",
		"notice":           0,
		"platform":         "yqq.json",
		"needNewCode":      0,
		"ct":               24,
		"cv":               0,
		"uin":              uin,
		"guid":             GenerateGUID(),
		"tpl":              "mini",
		"_":                GetMilliTimestamp(),
		"logininfo":        1,
		"fp_g_tk":          5381,
		"fp_loginUin":      uin,
		"fp_hostUin":       0,
		"fp_format":        "json",
		"fp_inCharset":     "utf8",
		"fp_outCharset":    "utf-8",
		"fp_notice":        0,
		"fp_platform":      "yqq.json",
		"fp_needNewCode":   0,
	}
}

// GtkHash calculates the g_tk hash from skey
func GtkHash(skey string) int {
	hash := 5381
	for i := 0; i < len(skey); i++ {
		hash += (hash << 5) + int(skey[i])
	}
	return hash & 0x7fffffff
}

// GenerateRandomString generates a random alphanumeric string of specified length
func GenerateRandomString(length int) string {
	const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
	rand.Seed(time.Now().UnixNano())
	result := make([]byte, length)
	for i := range result {
		result[i] = charset[rand.Intn(len(charset))]
	}
	return string(result)
}

// EncryptLoginToken encrypts the login token
func EncryptLoginToken(token string) string {
	// Simple XOR encryption with a key
	key := []byte("qqmusickey2023")
	encrypted := make([]byte, len(token))
	for i := 0; i < len(token); i++ {
		encrypted[i] = token[i] ^ key[i%len(key)]
	}
	return hex.EncodeToString(encrypted)
}

// DecryptLoginToken decrypts the login token
func DecryptLoginToken(encryptedHex string) (string, error) {
	encrypted, err := hex.DecodeString(encryptedHex)
	if err != nil {
		return "", err
	}
	key := []byte("qqmusickey2023")
	decrypted := make([]byte, len(encrypted))
	for i := 0; i < len(encrypted); i++ {
		decrypted[i] = encrypted[i] ^ key[i%len(key)]
	}
	return string(decrypted), nil
}

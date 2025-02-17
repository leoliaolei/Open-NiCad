/*-------------------------------------------------------------------------
 *
 * encode.c
 *       Various data encoding/decoding things.
 *
 * Copyright (c) 2001 PostgreSQL Global Development Group
 *
 *
 * IDENTIFICATION
 *       $Header: /cvsroot/pgsql/src/backend/utils/adt/encode.c,v 1.6 2001/11/05 17:46:29 momjian Exp $
 *
 *-------------------------------------------------------------------------
 */
#include "postgres.h"
#include <ctype.h>
#include "utils/builtins.h"
struct pg_encoding {
       unsigned      (*encode_len) (const uint8 *data, unsigned dlen);
       unsigned      (*decode_len) (const uint8 *data, unsigned dlen);
       unsigned      (*encode) (const uint8 *data, unsigned dlen, uint8 *res);
       unsigned      (*decode) (const uint8 *data, unsigned dlen, uint8 *res);
};
static struct pg_encoding *pg_find_encoding(const char *name);
/*
 * SQL functions.
 */
Datum
binary_encode(PG_FUNCTION_ARGS) {
       bytea    *data = PG_GETARG_BYTEA_P(0);
       Datum         name = PG_GETARG_DATUM(1);
       text     *result;
       char     *namebuf;
       int      datalen,
                      resultlen,
                      res;
       struct pg_encoding *enc;
       datalen = VARSIZE(data) - VARHDRSZ;
       namebuf = DatumGetCString(DirectFunctionCall1(textout, name));
       enc = pg_find_encoding(namebuf);
       if (enc == NULL)
             elog(ERROR, "No such encoding as '%s'", namebuf);
       resultlen = enc->encode_len(VARDATA(data), datalen);
       result = palloc(VARHDRSZ + resultlen);
       res = enc->encode(VARDATA(data), datalen, VARDATA(result));
       /* Make this FATAL 'cause we've trodden on memory ... */
       if (res > resultlen)
             elog(FATAL, "Overflow - encode estimate too small");
       VARATT_SIZEP(result) = VARHDRSZ + res;
       PG_RETURN_TEXT_P(result); }
Datum
binary_decode(PG_FUNCTION_ARGS) {
       text     *data = PG_GETARG_TEXT_P(0);
       Datum         name = PG_GETARG_DATUM(1);
       bytea    *result;
       char     *namebuf;
       int      datalen,
                      resultlen,
                      res;
       struct pg_encoding *enc;
       datalen = VARSIZE(data) - VARHDRSZ;
       namebuf = DatumGetCString(DirectFunctionCall1(textout, name));
       enc = pg_find_encoding(namebuf);
       if (enc == NULL)
             elog(ERROR, "No such encoding as '%s'", namebuf);
       resultlen = enc->decode_len(VARDATA(data), datalen);
       result = palloc(VARHDRSZ + resultlen);
       res = enc->decode(VARDATA(data), datalen, VARDATA(result));
       /* Make this FATAL 'cause we've trodden on memory ... */
       if (res > resultlen)
             elog(FATAL, "Overflow - decode estimate too small");
       VARATT_SIZEP(result) = VARHDRSZ + res;
       PG_RETURN_BYTEA_P(result); }
/*
 * HEX
 */
static const char *hextbl = "0123456789abcdef";
static const int8 hexlookup[128] = {
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       0, 1, 2, 3, 4, 5, 6, 7, 8, 9, -1, -1, -1, -1, -1, -1,
       -1, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       -1, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
};
static unsigned
hex_encode(const uint8 *src, unsigned len, uint8 *dst) {
       const uint8 *end = src + len;
       while (src < end) {
             *dst++ = hextbl[(*src >> 4) & 0xF];
             *dst++ = hextbl[*src & 0xF];
             src++; }
       return len * 2; }
static uint8
get_hex(unsigned c) {
       int      res = -1;
       if (c > 0 && c < 127)
             res = hexlookup[c];
       if (res < 0)
             elog(ERROR, "Bad hex code: '%c'", c);
       return (uint8) res; }
static unsigned
hex_decode(const uint8 *src, unsigned len, uint8 *dst) {
       const uint8 *s,
                     *srcend;
       uint8         v1,
                      v2,
                     *p = dst;
       srcend = src + len;
       s = src;
       p = dst;
       while (s < srcend) {
             if (*s == ' ' || *s == '\n' || *s == '\t' || *s == '\r') {
                  s++;
                  continue; }
             v1 = get_hex(*s++) << 4;
             if (s >= srcend)
                  elog(ERROR, "hex_decode: invalid data");
             v2 = get_hex(*s++);
             *p++ = v1 | v2; }
       return p - dst; }
static unsigned
hex_enc_len(const uint8 *src, unsigned srclen) {
       return srclen << 1; }
static unsigned
hex_dec_len(const uint8 *src, unsigned srclen) {
       return srclen >> 1; }
/*
 * BASE64
 */
static const unsigned char _base64[] =
"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
static const int8 b64lookup[128] = {
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,
       52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,
       -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
       15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,
       -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
       41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,
};
static unsigned
b64_encode(const uint8 *src, unsigned len, uint8 *dst) {
       uint8    *p,
                     *lend = dst + 76;
       const uint8 *s,
                     *end = src + len;
       int      pos = 2;
       uint32               buf = 0;
       s = src;
       p = dst;
       while (s < end) {
             buf |= *s << (pos << 3);
             pos--;
             s++;
             /* write it out */
             if (pos < 0) {
                  *p++ = _base64[(buf >> 18) & 0x3f];
                  *p++ = _base64[(buf >> 12) & 0x3f];
                  *p++ = _base64[(buf >> 6) & 0x3f];
                  *p++ = _base64[buf & 0x3f];
                  pos = 2;
                  buf = 0; }
             if (p >= lend) {
                  *p++ = '\n';
                  lend = p + 76; } }
       if (pos != 2) {
             *p++ = _base64[(buf >> 18) & 0x3f];
             *p++ = _base64[(buf >> 12) & 0x3f];
             *p++ = (pos == 0) ? _base64[(buf >> 6) & 0x3f] : '=';
             *p++ = '='; }
       return p - dst; }
static unsigned
b64_decode(const uint8 *src, unsigned len, uint8 *dst) {
       const char *srcend = src + len,
                     *s = src;
       uint8    *p = dst;
       unsigned      c;
       int      b = 0;
       uint32               buf = 0;
       int      pos = 0,
                      end = 0;
       while (s < srcend) {
             c = *s++;
             if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                  continue;
             if (c == '=') {
                  /* end sequence */
                  if (!end) {
                      if (pos == 2)
                         end = 1;
                      else if (pos == 3)
                         end = 2;
                      else
                         elog(ERROR, "base64: unexpected '='"); }
                  b = 0; }
             else {
                  b = -1;
                  if (c > 0 && c < 127)
                      b = b64lookup[c];
                  if (b < 0)
                      elog(ERROR, "base64: Invalid symbol"); }
             /* add it to buffer */
             buf = (buf << 6) + b;
             pos++;
             if (pos == 4) {
                  *p++ = (buf >> 16) & 255;
                  if (end == 0 || end > 1)
                      *p++ = (buf >> 8) & 255;
                  if (end == 0 || end > 2)
                      *p++ = buf & 255;
                  buf = 0;
                  pos = 0; } }
       if (pos != 0)
             elog(ERROR, "base64: invalid end sequence");
       return p - dst; }
static unsigned
b64_enc_len(const uint8 *src, unsigned srclen) {
       /* 3 bytes will be converted to 4, linefeed after 76 chars */
       return (srclen + 2) * 4 / 3 + srclen / (76 * 3 / 4); }
static unsigned
b64_dec_len(const uint8 *src, unsigned srclen) {
       return (srclen * 3) >> 2; }
/*
 * Escape
 * Minimally escape bytea to text.
 * De-escape text to bytea.
 *
 * Only two characters are escaped:
 * \0 (null) and \\ (backslash)
 *
 * De-escapes \\ and any \### octal
 */
#define VAL(CH)                     ((CH) - '0')
#define DIG(VAL)             ((VAL) + '0')
static unsigned
esc_encode(const uint8 *src, unsigned srclen, uint8 *dst) {
       const uint8 *end = src + srclen;
       uint8    *rp = dst;
       int      len = 0;
       while (src < end) {
             if (*src == '\0') {
                  rp[0] = '\\';
                  rp[1] = '0';
                  rp[2] = '0';
                  rp[3] = '0';
                  rp += 4;
                  len += 4; }
             else if (*src == '\\') {
                  rp[0] = '\\';
                  rp[1] = '\\';
                  rp += 2;
                  len += 2; }
             else {
                  *rp++ = *src;
                  len++; }
             src++; }
       return len; }
static unsigned
esc_decode(const uint8 *src, unsigned srclen, uint8 *dst) {
       const uint8 *end = src + srclen;
       uint8    *rp = dst;
       int      len = 0;
       while (src < end) {
             if (src[0] != '\\')
                  *rp++ = *src++;
             else if (src + 3 < end &&
                       (src[1] >= '0' && src[1] <= '3') &&
                       (src[2] >= '0' && src[2] <= '7') &&
                       (src[3] >= '0' && src[3] <= '7')) {
                  int                val;
                  val = VAL(src[1]);
                  val <<= 3;
                  val += VAL(src[2]);
                  val <<= 3;
                  *rp++ = val + VAL(src[3]);
                  src += 4; }
             else if (src + 1 < end &&
                       (src[1] == '\\')) {
                  *rp++ = '\\';
                  src += 2; }
             else {
                  /*
                   * One backslash, not followed by ### valid octal. Should
                   * never get here, since esc_dec_len does same check.
                   */
                  elog(ERROR, "decode: Bad input string for type bytea"); }
             len++; }
       return len; }
static unsigned
esc_enc_len(const uint8 *src, unsigned srclen) {
       const uint8 *end = src + srclen;
       int      len = 0;
       while (src < end) {
             if (*src == '\0')
                  len += 4;
             else if (*src == '\\')
                  len += 2;
             else
                  len++;
             src++; }
       return len; }
static unsigned
esc_dec_len(const uint8 *src, unsigned srclen) {
       const uint8 *end = src + srclen;
       int      len = 0;
       while (src < end) {
             if (src[0] != '\\')
                  src++;
             else if (src + 3 < end &&
                       (src[1] >= '0' && src[1] <= '3') &&
                       (src[2] >= '0' && src[2] <= '7') &&
                       (src[3] >= '0' && src[3] <= '7')) {
                  /*
                   * backslash + valid octal
                   */
                  src += 4; }
             else if (src + 1 < end &&
                       (src[1] == '\\')) {
                  /*
                   * two backslashes = backslash
                   */
                  src += 2; }
             else {
                  /*
                   * one backslash, not followed by ### valid octal
                   */
                  elog(ERROR, "decode: Bad input string for type bytea"); }
             len++; }
       return len; }
/*
 * Common
 */
static struct {
       const char *name;
       struct pg_encoding enc;
}      enclist[] = { {
             "hex", {
                  hex_enc_len, hex_dec_len, hex_encode, hex_decode }
       }, {
             "base64", {
                  b64_enc_len, b64_dec_len, b64_encode, b64_decode }
       }, {
             "escape", {
                  esc_enc_len, esc_dec_len, esc_encode, esc_decode }
       }, {
             NULL, {
                  NULL, NULL, NULL, NULL } }
};
static struct pg_encoding *
pg_find_encoding(const char *name) {
       int      i;
       for (i = 0; enclist[i].name; i++)
             if (strcasecmp(enclist[i].name, name) == 0)
                  return &enclist[i].enc;
       return NULL; }

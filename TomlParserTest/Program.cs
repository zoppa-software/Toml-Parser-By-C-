using System;
using System.Diagnostics;
using Toml;

namespace TomlParserTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var tomlData = @"
                title = ""TOML sample""
                ld1 = 1979-05-27
                lt1 = 07:32:00
                lt2 = 00:32:00.999999

                [database]
                server = ""192.168.1.1""
                ports = [8001, 8002, 8003]
                connection_max = 5000
                enabled = true

                [servers]

                # Indentation (tabs and/or spaces) is allowed but not required
                  [servers.alpha]
                  ip = ""10.0.0.1""
                  dc = ""eqdc10""

                  [servers.beta]
                  ip = ""10.0.0.2""
                  dc = ""eqdc10""

                [clients]
                data = [ [""gamma"", ""delta""], [1, 2] ]

                # Line breaks are OK when inside arrays
                hosts = [
                  ""alpha"",
                  ""omega""
                ]
                1234 = ""value""
                """" = ""blank""     # 可能ですがお奨めしません
            ";

            dynamic toml = new TomlDocument();
            toml.Load(tomlData);
            Console.WriteLine(toml);
            AreEqual((string)toml.title, "TOML sample");

            AreEqual((string)toml.database.server, "192.168.1.1");
            AreEqual((int)toml.database.ports[0], 8001);
            AreEqual((int)toml.database.ports[1], 8002);
            AreEqual((int)toml.database.ports[2], 8003);
            AreEqual((int)toml.database.connection_max, 5000);
            AreEqual((bool)toml.database.enabled, true);

            AreEqual((string)toml.servers.alpha.ip, "10.0.0.1");
            AreEqual((string)toml.servers.alpha.dc, "eqdc10");
            AreEqual((string)toml.servers.beta.ip, "10.0.0.2");
            AreEqual((string)toml.servers.beta.dc, "eqdc10");

            AreEqual((string)toml.clients.data[0][0], "gamma");
            AreEqual((string)toml.clients.data[0][1], "delta");
            AreEqual((int)toml.clients.data[1][0], 1);
            AreEqual((int)toml.clients.data[1][1], 2);
            AreEqual((string)toml.clients.hosts[0], "alpha");
            AreEqual((string)toml.clients.hosts[1], "omega");

            OneTable("tests\\example.toml");
            OneTable("tests\\table-array-nest.toml");
            OneTable("tests\\table-array-many.toml");
            OneTable("tests\\table-array-one.toml");
            OneTable("tests\\table-array-implicit.toml");
            OneTable("tests\\table-with-pound.toml");
            OneTable("tests\\table-whitespace.toml");
            OneTable("tests\\table-sub-empty.toml");
            OneTable("tests\\table-empty.toml");
            OneTable("tests\\implicit-groups.toml");
            OneTable("tests\\implicit-and-explicit-before.toml");
            OneTable("tests\\implicit-and-explicit-after.toml");
            OneTable("tests\\key-special-chars.toml");
            OneTable("tests\\key-space.toml");
            OneTable("tests\\key-equals-nospace.toml");
            OneTable("tests\\empty.toml");
            OneTable("tests\\dottrd-keys.toml");
            OneTable("tests\\comments-everywhere.toml");
            OneTable("tests\\arrays-nested.toml");
            OneTable("tests\\arrays-hetergeneous.toml");
            OneTable("tests\\arrays.toml");
            OneTable("tests\\array-nospaces.toml");
            OneTable("tests\\array-empty.toml");
            OneTable("tests\\float_no_number.toml");
            OneTable("tests\\inline.toml");
            OneTable("tests\\unicode-escape.toml");
            OneTable("tests\\unicode-literal.toml");
            OneTable("tests\\bool.toml");
            OneTable("tests\\float.toml");
            OneTable("tests\\integer.toml");
            OneTable("tests\\long-float.toml");
            OneTable("tests\\long-integer.toml");
            OneTable("tests\\number-formated-decimal.toml");
            OneTable("tests\\datetime.toml");
            OneTable("tests\\raw-string.toml");
            OneTable("tests\\string-empty.toml");
            OneTable("tests\\string-escapes.toml");
            OneTable("tests\\string-simple.toml");
            OneTable("tests\\string-with-pound.toml");
            OneTable("tests\\raw-multiline-string.toml");
            OneTable("tests\\multiline-string.toml");

            OneTable("invalid\\array-mixed-types-arrays-and-ints.toml");
            OneTable("invalid\\array-mixed-types-ints-and-floats.toml");
            OneTable("invalid\\array-mixed-types-strings-and-ints.toml");
            OneTable("invalid\\datetime-malformed-no-leads.toml");
            OneTable("invalid\\datetime-malformed-no-secs.toml");
            OneTable("invalid\\datetime-malformed-no-t.toml");
            OneTable("invalid\\datetime-malformed-with-milli.toml");
            OneTable("invalid\\duplicate-keys.toml");
            OneTable("invalid\\duplicate-key-table.toml");
            OneTable("invalid\\duplicate-tables.toml");
            OneTable("invalid\\empty-implicit-table.toml");
            OneTable("invalid\\empty-table.toml");
            OneTable("invalid\\float-no-leading-zero.toml");
            OneTable("invalid\\float-no-trailing-digits.toml");
            OneTable("invalid\\key-empty.toml");
            OneTable("invalid\\key-hash.toml");
            OneTable("invalid\\key-newline.toml");
            OneTable("invalid\\key-open-bracket.toml");
            OneTable("invalid\\key-single-open-bracket.toml");
            OneTable("invalid\\key-space.toml");
            OneTable("invalid\\key-start-bracket.toml");
            OneTable("invalid\\key-two-equals.toml");
            OneTable("invalid\\multiline-string-err.toml");
            OneTable("invalid\\number_2_1_err.toml");
            OneTable("invalid\\number_2_err.toml");
            OneTable("invalid\\number_8_1_err.toml");
            OneTable("invalid\\number_8_err.toml");
            OneTable("invalid\\number_16_1_err.toml");
            OneTable("invalid\\number_16_err.toml");
            OneTable("invalid\\string-bad-byte-escape.toml");
            OneTable("invalid\\string-bad-escape.toml");
            OneTable("invalid\\string-byte-escapes.toml");
            OneTable("invalid\\string-no-close.toml");
            OneTable("invalid\\table-array-implicit.toml");
            OneTable("invalid\\table-array-malformed-bracket.toml");
            OneTable("invalid\\table-array-malformed-empty.toml");
            OneTable("invalid\\table-empty.toml");
            OneTable("invalid\\table-nested-brackets-close.toml");
            OneTable("invalid\\table-nested-brackets-open.toml");
            OneTable("invalid\\table-whitespace.toml");
            OneTable("invalid\\table-with-pound.toml");
            OneTable("invalid\\text-after-array-entries.toml");
            OneTable("invalid\\text-after-integer.toml");
            OneTable("invalid\\text-after-string.toml");
            OneTable("invalid\\text-after-table.toml");
            OneTable("invalid\\text-before-array-separator.toml");
            OneTable("invalid\\text-in-array.toml");

            test_var0_5();
        }

        private static void test_var0_5()
        {
            dynamic toml1 = OneTable("tests\\example1.toml");
            AreEqual((string)toml1.title, "TOML Example");

            AreEqual((string)toml1.owner.name, "Tom Preston-Werner");
            AreEqual((string)toml1.owner.organization, "GitHub");
            AreEqual((string)toml1.owner.bio, "GitHub Cofounder & CEO\nLikes tater tots and beer.");
            AreEqual((TomlDate)toml1.owner.dob, new TomlDate(1979, 5, 27, 7, 32, 0, 0, 0, 0));

            AreEqual((string)toml1.database.server, "192.168.1.1");
            AreEqual((int)toml1.database.ports[0], 8001);
            AreEqual((int)toml1.database.ports[1], 8001);
            AreEqual((int)toml1.database.ports[2], 8002);
            AreEqual((int)toml1.database.connection_max, 5000);
            AreEqual((bool)toml1.database.enabled, true);

            AreEqual((string)toml1.servers.alpha.ip, "10.0.0.1");
            AreEqual((string)toml1.servers.alpha.dc, "eqdc10");
            AreEqual((string)toml1.servers.beta.ip, "10.0.0.2");
            AreEqual((string)toml1.servers.beta.dc, "eqdc10");
            AreEqual((string)toml1.servers.beta.country, "中国");

            AreEqual((string)toml1.clients.data[0][0], "gamma");
            AreEqual((string)toml1.clients.data[0][1], "delta");
            AreEqual((int)toml1.clients.data[1][0], 1);
            AreEqual((int)toml1.clients.data[1][1], 2);

            AreEqual((string)toml1.clients.hosts[0], "alpha");
            AreEqual((string)toml1.clients.hosts[1], "omega");

            AreEqual((string)toml1.products[0].name, "Hammer");
            AreEqual((int)toml1.products[0].sku, 738594937);
            AreEqual((string)toml1.products[1].name, "Nail");
            AreEqual((int)toml1.products[1].sku, 284758393);
            AreEqual((string)toml1.products[1].color, "gray");

            dynamic toml2 = OneTable("tests\\fruit.toml");
            AreEqual((string)toml2.fruit.blah[0].name, "apple");
            AreEqual((string)toml2.fruit.blah[0].physical.color, "red");
            AreEqual((string)toml2.fruit.blah[0].physical.shape, "round");
            AreEqual((string)toml2.fruit.blah[1].name, "banana");
            AreEqual((string)toml2.fruit.blah[1].physical.color, "yellow");
            AreEqual((string)toml2.fruit.blah[1].physical.shape, "bent");

            dynamic toml3 = OneTable("tests\\hard_example.toml");
            AreEqual((string)toml3.the.test_string, "You'll hate me after this - #");
            AreEqual((string)toml3.the.hard.test_array[0], "] ");
            AreEqual((string)toml3.the.hard.test_array[1], " # ");
            AreEqual((string)toml3.the.hard.test_array2[0], "Test #11 ]proved that");
            AreEqual((string)toml3.the.hard.test_array2[1], "Experiment #9 was a success");
            AreEqual((string)toml3.the.hard.another_test_string, " Same thing, but with a string #");
            AreEqual((string)toml3.the.hard.harder_test_string, " And when \"'s are in the string, along with # \"");
            AreEqual((string)((TomlDocument)toml3).Member("the").Member("hard").Member("bit#").Member("what?").Raw, "You don't think some user won't do that?");
            AreEqual((string)((TomlDocument)toml3).Member("the").Member("hard").Member("bit#").Member("multi_line_array")[0], "]");
        }

        private static TomlDocument OneTable(string path)
        {
            try {
                Console.WriteLine("---------- {0} ----------", path);
                var toml = new TomlDocument();
                toml.LoadPath(path);
                Console.WriteLine(toml);
                return toml;
            }
            catch (TomlAnalisysException e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private static void AreEqual(object left, object right)
        {
            if (!left.Equals(right)) {
                throw new InvalidOperationException();
            }
        }
    }
}
